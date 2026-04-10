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

    public enum OfferError
    {
        NOT_FOUND = -1,
        NOT_OWNER = -2,
        REQUEST_NOT_OPEN = -3
    }

    public enum ApproveOfferError
    {
        NOT_FOUND = -1,
        NOT_RENTER = -2,
        NO_PENDING_OFFER = -3,
        TRANSACTION_FAILED = -4
    }

    public enum DenyOfferError
    {
        NOT_FOUND = -1,
        NOT_RENTER = -2,
        NO_PENDING_OFFER = -3
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

        private const int BufferHours = 48;

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

            var bufferedStart = request.StartDate.AddHours(-BufferHours);
            var bufferedEnd = request.EndDate.AddHours(BufferHours);

            Rental rental;
            List<Request> overlappingRequests;

            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable);
            try
            {
                    rental = new Rental(
                        id: 0,
                        game: request.Game,
                        renter: request.Renter,
                        owner: request.Owner,
                        startDate: request.StartDate,
                        endDate: request.EndDate);

                    _rentalRepository.Add(rental, connection, transaction);

                    overlappingRequests = GetOverlappingRequests(
                        request.Game?.Id ?? 0, requestId, bufferedStart, bufferedEnd,
                        connection, transaction);

                    // Delete notifications BEFORE their referenced requests (FK constraint)
                    foreach (var overlap in overlappingRequests)
                    {
                        _notificationService.DeleteNotificationsByRequestId(overlap.Id);
                        _requestRepository.Delete(overlap.Id, connection, transaction);
                    }

                    _notificationService.DeleteNotificationsByRequestId(requestId);
                    _requestRepository.Delete(requestId, connection, transaction);
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    return (int)ApproveRequestError.TRANSACTION_FAILED_ERROR;
                }

                // Post-commit notifications
                foreach (var overlap in overlappingRequests)
                {
                    _notificationService.SendNotificationToUser(
                        overlap.Renter?.Id ?? 0,
                        new NotificationDTO
                        {
                            Id = 0,
                            User = new UserDTO { Id = overlap.Renter?.Id ?? 0 },
                            Timestamp = DateTime.UtcNow,
                            Title = "Booking Unavailable",
                            Body = $"Your request for {request.Game?.Name ?? "the selected game"} " +
                                   $"({overlap.StartDate:d}–{overlap.EndDate:d}) was declined " +
                                   $"because the game is no longer available in that period."
                        });
                }

                _notificationService.ScheduleUpcomingRentalReminder(rental);
                return rental.Id;
            }

            private static List<Request> GetOverlappingRequests(
                int gameId, int excludeRequestId,
                DateTime bufferedStart, DateTime bufferedEnd,
                SqlConnection connection, SqlTransaction transaction)
            {
                var list = new List<Request>();
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText =
                    "SELECT r.request_id, r.game_id, r.renter_id, r.owner_id, r.start_date, r.end_date, " +
                    "ru.display_name AS renter_display_name, ou.display_name AS owner_display_name, " +
                    "g.name AS game_name, g.image AS game_image " +
                    "FROM Requests r " +
                    "LEFT JOIN Users ru ON ru.id = r.renter_id " +
                    "LEFT JOIN Users ou ON ou.id = r.owner_id " +
                    "LEFT JOIN Games g ON g.game_id = r.game_id " +
                    "WHERE r.game_id = @game_id AND r.request_id != @exclude_id " +
                    "AND r.start_date < @buffered_end AND r.end_date > @buffered_start";
                command.Parameters.AddWithValue("@game_id", gameId);
                command.Parameters.AddWithValue("@exclude_id", excludeRequestId);
                command.Parameters.AddWithValue("@buffered_end", bufferedEnd);
                command.Parameters.AddWithValue("@buffered_start", bufferedStart);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var game = new Game
                    {
                        Id = (int)reader["game_id"],
                        Name = reader["game_name"] as string ?? string.Empty,
                        Image = reader["game_image"] as byte[] ?? Array.Empty<byte>()
                    };
                    var renter = new User((int)reader["renter_id"], reader["renter_display_name"] as string ?? string.Empty);
                    var owner = new User((int)reader["owner_id"], reader["owner_display_name"] as string ?? string.Empty);
                    list.Add(new Request((int)reader["request_id"], game, renter, owner,
                        (DateTime)reader["start_date"], (DateTime)reader["end_date"]));
                }
                return list;
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

            _notificationService.DeleteNotificationsByRequestId(requestId);
            _requestRepository.Delete(requestId);

            _notificationService.SendNotificationToUser(
                request.Renter?.Id ?? 0,
                new NotificationDTO
                {
                    Id = 0,
                    User = new UserDTO { Id = request.Renter?.Id ?? 0 },
                    Timestamp = DateTime.UtcNow,
                    Title = "Rental request declined",
                    Body = $"Your request for {request.Game?.Name ?? "the selected game"} " +
                           $"({request.StartDate:d}–{request.EndDate:d}) was declined. " +
                           $"Reason: {reason}"
                });

            return requestId;
        }

        // ----------------------------------------------------------------
        //  [BL-LFC-04] Renter cancels their own pending request
        // ----------------------------------------------------------------
        public void CancelRequest(int requestId)
        {
            try
            {
                _notificationService.DeleteNotificationsByRequestId(requestId);
                _requestRepository.Delete(requestId);
            }
            catch (KeyNotFoundException)
            {
                // Request was already deleted (e.g. by concurrent approval/offer).
                // The desired end-state — request gone — is satisfied.
            }
        }

        // ----------------------------------------------------------------
        //  [BL-LFC-05] Game deactivated — decline all pending requests
        // ----------------------------------------------------------------
        public void OnGameDeactivated(int gameId)
        {
            var pending = _requestRepository.GetRequestsByGame(gameId);

            foreach (var request in pending)
            {
                _notificationService.DeleteNotificationsByRequestId(request.Id);
                _requestRepository.Delete(request.Id);

                _notificationService.SendNotificationToUser(
                    request.Renter?.Id ?? 0,
                    new NotificationDTO
                    {
                        Id = 0,
                        User = new UserDTO { Id = request.Renter?.Id ?? 0 },
                        Timestamp = DateTime.UtcNow,
                        Title = "Rental request cancelled",
                        Body = $"Your request for {request.Game?.Name ?? "the selected game"} " +
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
            if (month == 0) month = DateTime.UtcNow.Month;
            if (year == 0) year = DateTime.UtcNow.Year;

            return _requestRepository
                .GetRequestsByGame(gameId)
                .Where(r => r.StartDate.Month == month && r.StartDate.Year == year)
                .OrderBy(r => r.StartDate)
                .Select(r => (r.StartDate, r.EndDate.AddHours(BufferHours)))
                .ToImmutableList();
        }

        // ----------------------------------------------------------------
        //  [API-CAV-04] Availability check
        // ----------------------------------------------------------------
        public bool CheckAvailability(int gameId, DateTime startDate, DateTime endDate)
        {
            var oneMonthFromNow = DateTime.UtcNow.AddMonths(1);
            if (startDate > oneMonthFromNow || endDate > oneMonthFromNow)
                return false;

            Game game;
            try { game = _gameRepository.Get(gameId); }
            catch (KeyNotFoundException) { return false; }

            if (!game.IsActive) return false;

            bool rentalConflict = _rentalRepository
                .GetRentalsByGame(gameId)
                .Any(r => startDate < r.EndDate.AddHours(BufferHours) &&
                          endDate > r.StartDate);

            if (rentalConflict) return false;

            bool requestConflict = _requestRepository
                .GetRequestsByGame(gameId)
                .Any(r => r.StartDate < endDate.AddHours(BufferHours) &&
                          r.EndDate.AddHours(BufferHours) > startDate);

            return !requestConflict;
        }

        // ----------------------------------------------------------------
        //  Owner offers their game to fulfill a request
        // ----------------------------------------------------------------
        public int OfferGame(int requestId, int offeringUserId)
        {
            Request request;
            try { request = _requestRepository.Get(requestId); }
            catch (KeyNotFoundException)
            { return (int)OfferError.NOT_FOUND; }

            if (request.Owner?.Id != offeringUserId)
                return (int)OfferError.NOT_OWNER;

            if (request.Status != RequestStatus.Open)
                return (int)OfferError.REQUEST_NOT_OPEN;

            _requestRepository.UpdateStatus(requestId, RequestStatus.OfferPending, offeringUserId);

            var ownerName = request.Owner?.DisplayName ?? "Someone";
            var gameName = request.Game?.Name ?? "a game";

            _notificationService.SendNotificationToUser(
                request.Renter?.Id ?? 0,
                new NotificationDTO
                {
                    Id = 0,
                    User = new UserDTO { Id = request.Renter?.Id ?? 0 },
                    Timestamp = DateTime.UtcNow,
                    Title = "Game Offer Received",
                    Body = $"{ownerName} is offering you {gameName} for {request.StartDate:dd/MM/yyyy} - {request.EndDate:dd/MM/yyyy}",
                    Type = NotificationType.OfferReceived,
                    RelatedRequestId = requestId
                });

            return requestId;
        }

        // ----------------------------------------------------------------
        //  Requester approves a pending offer — creates rental
        // ----------------------------------------------------------------
        public int ApproveOffer(int requestId, int renterId)
        {
            Request request;
            try { request = _requestRepository.Get(requestId); }
            catch (KeyNotFoundException)
            { return (int)ApproveOfferError.NOT_FOUND; }

            if (request.Renter?.Id != renterId)
                return (int)ApproveOfferError.NOT_RENTER;

            if (request.Status != RequestStatus.OfferPending)
                return (int)ApproveOfferError.NO_PENDING_OFFER;

            var bufferedStart = request.StartDate.AddHours(-BufferHours);
            var bufferedEnd = request.EndDate.AddHours(BufferHours);

            Rental rental;
            List<Request> overlappingRequests;

            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable);
            try
            {
                rental = new Rental(
                    id: 0,
                    game: request.Game,
                    renter: request.Renter,
                    owner: request.Owner,
                    startDate: request.StartDate,
                    endDate: request.EndDate);

                _rentalRepository.Add(rental, connection, transaction);

                overlappingRequests = GetOverlappingRequests(
                    request.Game?.Id ?? 0, requestId, bufferedStart, bufferedEnd,
                    connection, transaction);

                // Delete notifications BEFORE their referenced requests (FK constraint)
                foreach (var overlap in overlappingRequests)
                {
                    _notificationService.DeleteNotificationsByRequestId(overlap.Id);
                    _requestRepository.Delete(overlap.Id, connection, transaction);
                }

                _notificationService.DeleteNotificationsByRequestId(requestId);
                _requestRepository.Delete(requestId, connection, transaction);
                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                return (int)ApproveOfferError.TRANSACTION_FAILED;
            }

            // Post-commit: send notifications (outside transaction so failures don't mask success)
            var ownerName = request.Owner?.DisplayName ?? "The owner";
            var renterName = request.Renter?.DisplayName ?? "The requester";
            var gameName = request.Game?.Name ?? "a game";

            foreach (var overlap in overlappingRequests)
            {
                _notificationService.SendNotificationToUser(
                    overlap.Renter?.Id ?? 0,
                    new NotificationDTO
                    {
                        Id = 0,
                        User = new UserDTO { Id = overlap.Renter?.Id ?? 0 },
                        Timestamp = DateTime.UtcNow,
                        Title = "Booking Unavailable",
                        Body = $"Your request for {gameName} " +
                               $"({overlap.StartDate:d}-{overlap.EndDate:d}) was declined " +
                               $"because the game is no longer available in that period."
                    });
            }

            _notificationService.SendNotificationToUser(
                request.OfferingUser?.Id ?? request.Owner?.Id ?? 0,
                new NotificationDTO
                {
                    Id = 0,
                    User = new UserDTO { Id = request.OfferingUser?.Id ?? request.Owner?.Id ?? 0 },
                    Timestamp = DateTime.UtcNow,
                    Title = "Offer Accepted",
                    Body = $"{renterName} accepted your offer for {gameName}",
                    Type = NotificationType.OfferResult
                });

            _notificationService.SendNotificationToUser(
                renterId,
                new NotificationDTO
                {
                    Id = 0,
                    User = new UserDTO { Id = renterId },
                    Timestamp = DateTime.UtcNow,
                    Title = "Rental Confirmed",
                    Body = $"You accepted the offer for {gameName} from {ownerName}",
                    Type = NotificationType.OfferResult
                });

            _notificationService.ScheduleUpcomingRentalReminder(rental);
            return rental.Id;
        }

        // ----------------------------------------------------------------
        //  Requester denies a pending offer — resets request to Open
        // ----------------------------------------------------------------
        public int DenyOffer(int requestId, int renterId)
        {
            Request request;
            try { request = _requestRepository.Get(requestId); }
            catch (KeyNotFoundException)
            { return (int)DenyOfferError.NOT_FOUND; }

            if (request.Renter?.Id != renterId)
                return (int)DenyOfferError.NOT_RENTER;

            if (request.Status != RequestStatus.OfferPending)
                return (int)DenyOfferError.NO_PENDING_OFFER;

            // Reset request to Open so new offers can be made
            _requestRepository.UpdateStatus(requestId, RequestStatus.Open, null);

            // Clean up actionable notifications for this request
            _notificationService.DeleteNotificationsByRequestId(requestId);

            var renterName = request.Renter?.DisplayName ?? "The requester";
            var ownerName = request.Owner?.DisplayName ?? "The owner";
            var gameName = request.Game?.Name ?? "a game";

            // Notify owner: offer was denied
            _notificationService.SendNotificationToUser(
                request.OfferingUser?.Id ?? request.Owner?.Id ?? 0,
                new NotificationDTO
                {
                    Id = 0,
                    User = new UserDTO { Id = request.OfferingUser?.Id ?? request.Owner?.Id ?? 0 },
                    Timestamp = DateTime.UtcNow,
                    Title = "Offer Denied",
                    Body = $"{renterName} denied your offer for {gameName}",
                    Type = NotificationType.OfferResult
                });

            // Notify renter: confirmation of denial
            _notificationService.SendNotificationToUser(
                renterId,
                new NotificationDTO
                {
                    Id = 0,
                    User = new UserDTO { Id = renterId },
                    Timestamp = DateTime.UtcNow,
                    Title = "Offer Declined",
                    Body = $"You declined the offer for {gameName} from {ownerName}",
                    Type = NotificationType.OfferResult
                });

            return requestId;
        }
    }
}
