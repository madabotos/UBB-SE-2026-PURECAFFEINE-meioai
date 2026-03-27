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
        //  Dependencies
        // ----------------------------------------------------------------
        private readonly IRequestRepository _requestRepository;
        private readonly IRentalRepository _rentalRepository;
        private readonly INotificationService _notificationService;
        private readonly IGameRepository _gameRepository;
        private readonly IMapper<Request, RequestDTO> _requestMapper;

        private const int s_bufferPeriodInDays = 2;

        private readonly string _connectionString =
            System.Configuration.ConfigurationManager
                  .ConnectionStrings["BoardRent"]?.ConnectionString ?? string.Empty;

        // ----------------------------------------------------------------
        //  Constructor injection — DI container handles everything
        // ----------------------------------------------------------------
        public RequestService(
            IRequestRepository requestRepository,
            IRentalRepository rentalRepository,
            IGameRepository gameRepository,
            INotificationService notificationService,
            IMapper<Request, RequestDTO> requestMapper)
        {
            _requestRepository = requestRepository;
            _rentalRepository = rentalRepository;
            _gameRepository = gameRepository;
            _notificationService = notificationService;
            _requestMapper = requestMapper;
        }

        // ----------------------------------------------------------------
        //  Queries
        // ----------------------------------------------------------------
        public ImmutableList<RequestDTO> GetRequestsForRenter(int renterId) =>
            _requestRepository
                .GetRequestsByRenter(renterId)
                .Select(r => _requestMapper.ToDTO(r))
                .ToImmutableList();

        public ImmutableList<RequestDTO> GetRequestsForOwner(int ownerId) =>
            _requestRepository
                .GetRequestsByOwner(ownerId)
                .Select(r => _requestMapper.ToDTO(r))
                .ToImmutableList();

        // ----------------------------------------------------------------
        //  [BL-LFC-01] Create a new PENDING request
        // ----------------------------------------------------------------
        public int CreateRequest(int gameId, int renterId, int ownerId,
                                 DateTime startDate, DateTime endDate)
        {
            if (renterId == ownerId)
                return (int)CreateRequestError.OWNER_CANNOT_RENT_ERROR;

            try { _gameRepository.Get(gameId); }
            catch (KeyNotFoundException)
            { return (int)CreateRequestError.GAMEID_DOES_NOT_EXIST_ERROR; }

            if (!CheckAvailability(gameId, startDate, endDate))
                return (int)CreateRequestError.DATES_UNAVAILABLE_ERROR;

            var request = new Request(
                id: 0,
                game: new Game { Id = gameId },
                renter: new User { Id = renterId },
                owner: new User { Id = ownerId },
                startDate: startDate,
                endDate: endDate);

            _requestRepository.Add(request);
            return request.Id;
        }

        // ----------------------------------------------------------------
        //  [BL-LFC-02] Owner accepts a request — must be fully atomic
        // ----------------------------------------------------------------
        public int ApproveRequest(int requestId, int ownerId)
        {
            Request request;
            try { request = _requestRepository.Get(requestId); }
            catch (KeyNotFoundException)
            { return (int)ApproveRequestError.NOT_FOUND_ERROR; }

            if (request.Owner?.Id != ownerId)
                return (int)ApproveRequestError.UNAUTHORIZED_ERROR;

            var bufferedStart = request.StartDate.AddHours(-48);
            var bufferedEnd = request.EndDate.AddHours(48);

            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var rental = new Rental(
                    id: 0,
                    game: request.Game,
                    renter: request.Renter,
                    owner: request.Owner,
                    startDate: request.StartDate,
                    endDate: request.EndDate);

                _rentalRepository.Add(rental);

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
                            Id = 0,
                            User = new UserDTO { Id = overlap.Renter?.Id ?? 0 },
                            Timestamp = DateTime.UtcNow,
                            Title = "Booking Unavailable",
                            Body = $"Your request for game {request.Game?.Id} " +
                                   $"({overlap.StartDate:d}–{overlap.EndDate:d}) was declined " +
                                   $"because the game is no longer available in that period."
                        });
                }

                _requestRepository.Delete(requestId);
                transaction.Commit();

                _notificationService.ScheduleUpcomingRentalReminder(rental);
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
            try { request = _requestRepository.Get(requestId); }
            catch (KeyNotFoundException)
            { return (int)DenyRequestError.NOT_FOUND_ERROR; }

            if (request.Owner?.Id != ownerId)
                return (int)DenyRequestError.UNAUTHORIZED_ERROR;

            _requestRepository.Delete(requestId);

            _notificationService.SendNotificationToUser(
                request.Renter?.Id ?? 0,
                new NotificationDTO
                {
                    Id = 0,
                    User = new UserDTO { Id = request.Renter?.Id ?? 0 },
                    Timestamp = DateTime.Now,
                    Title = "Rental request declined",
                    Body = $"Your request for game {request.Game?.Id} " +
                           $"({request.StartDate:d}–{request.EndDate:d}) was declined. " +
                           $"Reason: {reason}"
                });

            return requestId;
        }

        // ----------------------------------------------------------------
        //  [BL-LFC-04] Renter cancels their own pending request
        // ----------------------------------------------------------------
        public void CancelRequest(int requestId) =>
            _requestRepository.Delete(requestId);

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
                    new NotificationDTO
                    {
                        Id = 0,
                        User = new UserDTO { Id = request.Renter?.Id ?? 0 },
                        Timestamp = DateTime.Now,
                        Title = "Rental request cancelled",
                        Body = $"Your request for game {gameId} " +
                               $"({request.StartDate:d}–{request.EndDate:d}) has been cancelled " +
                               $"because the game is no longer available."
                    });
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
                .Select(r => (r.StartDate, r.EndDate.AddHours(48)))
                .ToImmutableList();
        }

        // ----------------------------------------------------------------
        //  [API-CAV-04] Availability check
        // ----------------------------------------------------------------
        public bool CheckAvailability(int gameId, DateTime startDate, DateTime endDate)
        {
            var oneMonthFromNow = DateTime.Now.AddMonths(1);
            if (startDate > oneMonthFromNow || endDate > oneMonthFromNow)
                return false;

            Game game;
            try { game = _gameRepository.Get(gameId); }
            catch (KeyNotFoundException) { return false; }

            if (!game.IsActive) return false;

            bool rentalConflict = _rentalRepository
                .GetRentalsByGame(gameId)
                .Any(r => startDate < r.EndDate.AddHours(48) &&
                          endDate > r.StartDate);

            if (rentalConflict) return false;

            bool requestConflict = _requestRepository
                .GetRequestsByGame(gameId)
                .Any(r => r.StartDate < endDate.AddDays(s_bufferPeriodInDays) &&
                          r.EndDate.AddDays(s_bufferPeriodInDays) > startDate);

            return !requestConflict;
        }
    }
}
