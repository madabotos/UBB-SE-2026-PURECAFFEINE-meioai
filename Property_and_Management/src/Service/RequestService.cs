using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Transactions;
using Microsoft.Data.SqlClient;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Service
{
    public enum CreateRequestError
    {
        OWNER_CANNOT_RENT_ERROR = -1,
        DATES_UNAVAILABLE_ERROR = -2,
        GAMEID_DOES_NOT_EXIST_ERROR = -3
    }

    public enum ApproveRequestError
    {
        UNAUTHORIZED_ERROR = -1,
        NOT_FOUND_ERROR = -2,
        TRANSACTION_FAILED_ERROR = -3
    }

    public enum DenyRequestError
    {
        UNAUTHORIZED_ERROR = -1,
        NOT_FOUND_ERROR = -2
    }

    public class RequestService : IRequestService
    {
        // ----------------------------------------------------------------
        //  Dependencies — all held as interfaces for testability
        // ----------------------------------------------------------------
        private IRequestRepository _requestRepository;
        private IRentalRepository _rentalRepository;
        private INotificationService _notificationService;
        private IGameRepository _gameRepository;

        private readonly string _connectionString =
            System.Configuration.ConfigurationManager
                  .ConnectionStrings["BoardRent"]?.ConnectionString ?? string.Empty;

        // ----------------------------------------------------------------
        //  Dependency setters (poor-man's DI; replace with constructor DI later)
        // ----------------------------------------------------------------
        public void SetRequestRepository(IRequestRepository repo) => _requestRepository = repo;
        public void SetRentalRepository(IRentalRepository repo) => _rentalRepository = repo;
        public void SetNotificationService(INotificationService svc) => _notificationService = svc;
        public void SetGameRepository(IGameRepository repo) => _gameRepository = repo;

        // ----------------------------------------------------------------
        //  Queries
        // ----------------------------------------------------------------

        public ImmutableList<RequestDTO> GetRequestsForRenter(int renterId) =>
            _requestRepository
                .GetRequestsByRenter(renterId)
                .Select(r => new RequestDTO(r))
                .ToImmutableList();

        public ImmutableList<RequestDTO> GetRequestsForOwner(int ownerId) =>
            _requestRepository
                .GetRequestsByOwner(ownerId)
                .Select(r => new RequestDTO(r))
                .ToImmutableList();

        // ----------------------------------------------------------------
        //  [BL-LFC-01] Create a new PENDING request
        // ----------------------------------------------------------------
        public int CreateRequest(int gameId, int renterId, int ownerId,
                                 DateTime startDate, DateTime endDate)
        {
            // An Owner cannot rent their own game
            if (renterId == ownerId)
                return (int)CreateRequestError.OWNER_CANNOT_RENT_ERROR;

            // The GameID must exist in the database
            try
            {
                _gameRepository.Get(gameId);
            }
            catch (KeyNotFoundException)
            {
                return (int)CreateRequestError.GAMEID_DOES_NOT_EXIST_ERROR;
            }

            // The requested dates must be available
            if (!CheckAvailability(gameId, startDate, endDate))
                return (int)CreateRequestError.DATES_UNAVAILABLE_ERROR;

            // If all checks pass, we create the Request object in memory
            var request = new Request(
                id: 0,
                game: new Game { Id = gameId },
                renter: new User { Id = renterId },
                owner: new User { Id = ownerId },
                startDate: startDate,
                endDate: endDate);

            // Tell the repo to execute the raw SQL INSERT
            _requestRepository.Add(request);

            // Not sure why we wrote the return this way, so I changed it so we do not have efficiency problems (downloading every single request for that renter) and race conditions (from the .Last() if two users hit Rent at the same time)
            /*return _requestRepository
                .GetRequestsByRenter(renterId)
                .Last(r => r.Game?.Id == gameId &&
                   r.StartDate == startDate &&
                   r.EndDate == endDate)
                .Id;*/
            return request.Id;
        }

        // ----------------------------------------------------------------
        //  [BL-LFC-02] Owner accepts a request — must be fully atomic
        // ----------------------------------------------------------------
        public int ApproveRequest(int requestId, int ownerId)
        {
            // Fetch the request first (outside the transaction — read-only)
            Request request;
            // Check if the request exists
            try { request = _requestRepository.Get(requestId); }
            catch (KeyNotFoundException)
            { return (int)ApproveRequestError.NOT_FOUND_ERROR; }

            // Check if the person approving the request is the owner of the game
            if (request.Owner?.Id != ownerId)
                return (int)ApproveRequestError.UNAUTHORIZED_ERROR;

            // 48-hour buffer used to find overlapping pending requests
            var bufferedStart = request.StartDate.AddHours(-48);
            var bufferedEnd = request.EndDate.AddHours(48);

            // Collect overlapping requests before opening the transaction
            var overlapping = _requestRepository
                .GetRequestsByGame(request.Game?.Id ?? 0)
                .Where(r => r.Id != requestId &&
                            r.StartDate < bufferedEnd &&
                            r.EndDate > bufferedStart)
                .ToList();

            // Open a single connection + transaction for all mutating steps (a)–(d)
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                // Create a new Rental entity using the data from the approved Reuqest
                var rental = new Rental(
                id: 0,
                game: request.Game,
                renter: request.Renter,
                owner: request.Owner,
                startDate: request.StartDate,
                endDate: request.EndDate);

                _rentalRepository.Add(rental);

                // Find and delete all OTHER requests that overlap 
                var overlappingRequests = _requestRepository
                    .GetRequestsByGame(request.Game?.Id ?? 0)
                    .Where(r => r.Id != requestId &&
                                r.StartDate < bufferedEnd &&
                                r.EndDate > bufferedStart)
                    .ToList();

                foreach (var overlap in overlappingRequests)
                {
                    _requestRepository.Delete(overlap.Id);
                    _notificationService.SendNotificationToUser(
                        overlap.Renter?.Id ?? 0,
                        new NotificationDTO
                        {
                            Title = "Booking Unavailable",
                            Body = $"Your request for game {request.Game?.Id} ({overlap.StartDate:d}–{overlap.EndDate:d}) was declined because the game is no longer available in that period.",
                            Timestamp = DateTime.UtcNow
                        }
                    );
                }

                // Delete the original request
                _requestRepository.Delete(requestId);

                // Commiting the transaction. If any of the above operations threw an exception, this line will not be reached and the transaction will be rolled back.
                transaction.Commit();

                // 

                // Schedule sending of message
                _notificationService.ScheduleUpcomingRentalReminder(rental);

                // Return newly generated rental_id
                return rental.Id;
            }
            catch (Exception)
            {
                transaction.Rollback();
                return (int)ApproveRequestError.TRANSACTION_FAILED_ERROR;
            }
        }

        // ----------------------------------------------------------------
        //  [BL-LFC-03] Owner declines a request
        // ----------------------------------------------------------------
        public int DenyRequest(int requestId, int ownerId, string reason)
        {
            Request request;
            // Check if the request exists
            try { request = _requestRepository.Get(requestId); }
            catch (KeyNotFoundException)
            { return (int)DenyRequestError.NOT_FOUND_ERROR; }

            // Check if the person declining the request is the owner of the game
            if (request.Owner?.Id != ownerId)
                return (int)DenyRequestError.UNAUTHORIZED_ERROR;

            // Delete the request
            _requestRepository.Delete(requestId);

            _notificationService.SendNotificationToUser(
                request.Renter?.Id ?? 0,
                new NotificationDTO(
                    id: 0,
                    user: request.Renter,
                    timestamp: DateTime.Now,
                    title: "Rental request declined",
                    body: $"Your request for game {request.Game?.Id} " +
                               $"({request.StartDate:d}–{request.EndDate:d}) was declined. " +
                               $"Reason: {reason}"));

            return requestId;
        }

        // ----------------------------------------------------------------
        //  [BL-LFC-04] Renter cancels their own pending request
        // ----------------------------------------------------------------
        public void CancelRequest(int requestId)
        {
            _requestRepository.Delete(requestId);
        }

        // ----------------------------------------------------------------
        //  [BL-LFC-05] Game deactivated — decline all pending requests
        // ----------------------------------------------------------------
        public void OnGameDeactivated(int gameId)
        {
            var pending = _requestRepository.GetRequestsByGame(gameId);

            foreach (var request in pending)
            {
                _requestRepository.Delete(request.Id);

                _notificationService.SendNotificationToUser(
                    request.Renter?.Id ?? 0,
                    new NotificationDTO(
                        id: 0,
                        user: request.Renter,
                        timestamp: DateTime.Now,
                        title: "Rental request cancelled",
                        body: $"Your request for game {gameId} " +
                                   $"({request.StartDate:d}–{request.EndDate:d}) has been cancelled " +
                                   $"because the game is no longer available."));
            }
        }

        // ----------------------------------------------------------------
        //  [API-GBD-04 / API-GBD-05] Get booked date ranges for a game
        // ----------------------------------------------------------------
        public ImmutableList<(DateTime, DateTime)> GetBookedDates(int gameId,
                                                                   int month = 0,
                                                                   int year = 0)
        {
            if (month == 0) month = DateTime.Now.Month;
            if (year == 0) year = DateTime.Now.Year;

            return _requestRepository
                .GetRequestsByGame(gameId)
                .Where(r => r.StartDate.Month == month && r.StartDate.Year == year)
                .OrderBy(r => r.StartDate)
                .Select(r => (r.StartDate, r.EndDate.AddHours(48))) // 48-hour buffer per spec
                .ToImmutableList();
        }

        // ----------------------------------------------------------------
        //  [API-CAV-04] Availability check
        //  TRUE only if ALL of:
        //  (a) no overlap with existing Rentals (including their 48h buffer)
        //  (b) no overlap with existing pending Requests
        //  (c) startDate is not more than 1 month in the future
        //  (d) endDate   is not more than 1 month in the future
        //  (e) game exists and is active
        // ----------------------------------------------------------------
        public bool CheckAvailability(int gameId, DateTime startDate, DateTime endDate)
        {
            // (c) + (d) Both dates must be within the next month
            var oneMonthFromNow = DateTime.Now.AddMonths(1);
            if (startDate > oneMonthFromNow || endDate > oneMonthFromNow)
                return false;

            // (e) Game must exist and be active
            Game game;
            try { game = _gameRepository.Get(gameId); }
            catch (KeyNotFoundException) { return false; }

            if (!game.IsActive)
                return false;

            // (a) No overlap with confirmed Rentals (+ 48h buffer on rental end)
            bool rentalConflict = _rentalRepository
                .GetRentalsByGame(gameId)
                .Any(r => startDate < r.EndDate.AddHours(48) &&
                          endDate > r.StartDate);

            if (rentalConflict) return false;

            // (b) No overlap with other pending Requests
            bool requestConflict = _requestRepository
                .GetRequestsByGame(gameId)
                .Any(r => startDate < r.EndDate &&
                          endDate > r.StartDate);

            return !requestConflict;
        }
    }
}
