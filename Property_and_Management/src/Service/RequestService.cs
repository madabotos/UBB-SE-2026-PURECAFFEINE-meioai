using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
            // A user cannot rent their own game
            if (renterId == ownerId)
                return (int)CreateRequestError.OWNER_CANNOT_RENT_ERROR;

            // The game must exist and be active
            try { _gameRepository.Get(gameId); }
            catch (KeyNotFoundException)
            { return (int)CreateRequestError.GAMEID_DOES_NOT_EXIST_ERROR; }

            // Dates must be available (checks rentals + requests + 48h buffer + 1-month cap)
            if (!CheckAvailability(gameId, startDate, endDate))
                return (int)CreateRequestError.DATES_UNAVAILABLE_ERROR;

            var request = new Request(
                0,
                new Game { Id = gameId },
                new User { Id = renterId },
                new User { Id = ownerId },
                startDate,
                endDate);

            _requestRepository.Add(request);
            return request.Id; // Id is set by the repository via SCOPE_IDENTITY()
        }

        // ----------------------------------------------------------------
        //  [BL-LFC-02] Owner accepts a request — must be fully atomic
        // ----------------------------------------------------------------
        public int ApproveRequest(int requestId, int ownerId)
        {
            // Fetch the request first (outside the transaction — read-only)
            Request request;
            try { request = _requestRepository.Get(requestId); }
            catch (KeyNotFoundException)
            { return (int)ApproveRequestError.NOT_FOUND_ERROR; }

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
                // (a) Create the rental
                var rental = new Rental(
                    0, request.Game, request.Renter,
                    request.Owner, request.StartDate, request.EndDate);

                _rentalRepository.Add(rental); // TODO: add transaction overload to IRentalRepository

                // (b) + (c) Delete overlapping requests and notify their renters
                foreach (var overlap in overlapping)
                {
                    _notificationService.SendNotificationToUser(
                        overlap.Renter?.Id ?? 0,
                        new NotificationDTO(
                            id: 0,
                            user: overlap.Renter,
                            timestamp: DateTime.Now,
                            title: "Rental request unavailable",
                            body: $"Your request for game {overlap.Game?.Id} " +
                                       $"({overlap.StartDate:d}–{overlap.EndDate:d}) could not be fulfilled " +
                                       $"because the game was booked by another user. " +
                                       $"You can make a new booking at: /booking/{overlap.Game?.Id}"));

                    _requestRepository.Delete(overlap.Id, connection, transaction);
                }

                // (d) Delete the accepted request itself
                _requestRepository.Delete(requestId, connection, transaction);

                transaction.Commit();
                return rental.Id;
            }
            catch
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
            try { request = _requestRepository.Get(requestId); }
            catch (KeyNotFoundException)
            { return (int)DenyRequestError.NOT_FOUND_ERROR; }

            if (request.Owner?.Id != ownerId)
                return (int)DenyRequestError.UNAUTHORIZED_ERROR;

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
