using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Property_and_Management;
using Property_and_Management.src.DataTransferObjects;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Service
{
    public enum CreateRequestError
    {
        OWNER_CANNOT_RENT_ERROR = -1,
        DATES_UNAVAILABLE_ERROR = -2,
        GAME_ID_DOES_NOT_EXIST_ERROR = -3
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
        private readonly IRequestRepository _requestRepository;
        private readonly IRentalRepository _rentalRepository;
        private readonly INotificationService _notificationService;
        private readonly IGameRepository _gameRepository;
        private readonly IMapper<Request, RequestDataTransferObject> _requestMapper;

        private const int BufferHours = 48;
        private const int NewEntityId = 0;
        private const int MissingUserId = 0;
        private const int MissingOptionalDatePart = 0;
        private const int AvailabilityWindowMonths = 1;
        private const int MinimumSuccessfulEntityId = 1;

        public RequestService(
            IRequestRepository requestRepository,
            IRentalRepository rentalRepository,
            IGameRepository gameRepository,
            INotificationService notificationService,
            IMapper<Request, RequestDataTransferObject> requestMapper)
        {
            _requestRepository = requestRepository;
            _rentalRepository = rentalRepository;
            _gameRepository = gameRepository;
            _notificationService = notificationService;
            _requestMapper = requestMapper;
        }

        public ImmutableList<RequestDataTransferObject> GetRequestsForRenter(int renterId) =>
            _requestRepository
                .GetRequestsByRenter(renterId)
                .Select(request => _requestMapper.ToDataTransferObject(request))
                .ToImmutableList();

        public ImmutableList<RequestDataTransferObject> GetRequestsForOwner(int ownerId) =>
            _requestRepository
                .GetRequestsByOwner(ownerId)
                .Select(request => _requestMapper.ToDataTransferObject(request))
                .ToImmutableList();

        // [BL-LFC-01] Create a new PENDING request
        public int CreateRequest(int gameId, int renterId, int ownerId,
                                 DateTime startDate, DateTime endDate)
        {
            if (renterId == ownerId)
                return (int)CreateRequestError.OWNER_CANNOT_RENT_ERROR;

            try { _gameRepository.Get(gameId); }
            catch (KeyNotFoundException)
            { return (int)CreateRequestError.GAME_ID_DOES_NOT_EXIST_ERROR; }

            if (!CheckAvailability(gameId, startDate, endDate))
                return (int)CreateRequestError.DATES_UNAVAILABLE_ERROR;

            var request = new Request(
                id: NewEntityId,
                game: new Game { Id = gameId },
                renter: new User { Id = renterId },
                owner: new User { Id = ownerId },
                startDate: startDate,
                endDate: endDate);

            _requestRepository.Add(request);
            return request.Id;
        }

        // [BL-LFC-02] Owner accepts a request — fully atomic via repository
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

            int rentalId;
            ImmutableList<Request> overlappingRequests;

            try
            {
                (rentalId, overlappingRequests) = _requestRepository.ApproveAtomically(
                    request, bufferedStart, bufferedEnd);
            }
            catch
            {
                return (int)ApproveRequestError.TRANSACTION_FAILED_ERROR;
            }

            NotifyOverlappingRequestsUnavailable(overlappingRequests, request.Game?.Name ?? "the selected game");

            _notificationService.ScheduleUpcomingRentalReminder(
                request.Renter?.Id ?? MissingUserId,
                request.Owner?.Id ?? MissingUserId,
                request.Game?.Name ?? "your game",
                request.StartDate);
            return rentalId;
        }

        // [BL-LFC-03] Owner declines a request
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

            var renterId = request.Renter?.Id ?? MissingUserId;
            var gameName = request.Game?.Name ?? "the selected game";
            SendNotificationToUser(
                renterId,
                Constants.NotificationTitles.RentalRequestDeclined,
                $"Your request for {gameName} {FormatRequestPeriod(request.StartDate, request.EndDate)} was declined. Reason: {reason}");

            return requestId;
        }

        // [BL-LFC-04] Renter cancels their own pending request
        public void CancelRequest(int requestId)
        {
            try
            {
                _notificationService.DeleteNotificationsByRequestId(requestId);
                _requestRepository.Delete(requestId);
            }
            catch (KeyNotFoundException) { }
        }

        // [BL-LFC-05] Game deactivated — decline all pending requests
        public void OnGameDeactivated(int gameId)
        {
            var pending = _requestRepository
                .GetRequestsByGame(gameId)
                .Where(request => request.Status == RequestStatus.Open || request.Status == RequestStatus.OfferPending)
                .ToImmutableList();

            foreach (var request in pending)
            {
                _notificationService.DeleteNotificationsByRequestId(request.Id);
                _requestRepository.Delete(request.Id);

                var renterId = request.Renter?.Id ?? MissingUserId;
                var gameName = request.Game?.Name ?? "the selected game";
                SendNotificationToUser(
                    renterId,
                    Constants.NotificationTitles.RentalRequestCancelled,
                    $"Your request for {gameName} {FormatRequestPeriod(request.StartDate, request.EndDate)} has been cancelled because the game is no longer available.");
            }
        }

        public ImmutableList<(DateTime, DateTime)> GetBookedDates(
            int gameId,
            int month = MissingOptionalDatePart,
            int year = MissingOptionalDatePart)
        {
            if (month == MissingOptionalDatePart) month = DateTime.UtcNow.Month;
            if (year == MissingOptionalDatePart) year = DateTime.UtcNow.Year;

            return _requestRepository
                .GetRequestsByGame(gameId)
                .Where(request => request.StartDate.Month == month && request.StartDate.Year == year)
                .OrderBy(request => request.StartDate)
                .Select(request => (request.StartDate, request.EndDate.AddHours(BufferHours)))
                .ToImmutableList();
        }

        public bool CheckAvailability(int gameId, DateTime startDate, DateTime endDate)
        {
            var oneMonthFromNow = DateTime.UtcNow.AddMonths(AvailabilityWindowMonths);
            if (startDate > oneMonthFromNow || endDate > oneMonthFromNow)
                return false;

            Game game;
            try { game = _gameRepository.Get(gameId); }
            catch (KeyNotFoundException) { return false; }

            if (!game.IsActive) return false;

            bool rentalConflict = _rentalRepository
                .GetRentalsByGame(gameId)
                .Any(rental => startDate < rental.EndDate.AddHours(BufferHours) &&
                               endDate > rental.StartDate);

            if (rentalConflict) return false;

            bool requestConflict = _requestRepository
                .GetRequestsByGame(gameId)
                .Any(request => request.StartDate < endDate.AddHours(BufferHours) &&
                                request.EndDate.AddHours(BufferHours) > startDate);

            return !requestConflict;
        }

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

            var rentalId = ApproveRequest(requestId, offeringUserId);
            if (rentalId < MinimumSuccessfulEntityId)
                return rentalId;

            var renterId = request.Renter?.Id ?? MissingUserId;
            var gameName = request.Game?.Name ?? "the selected game";
            SendNotificationToUser(
                renterId,
                Constants.NotificationTitles.RentalRequestApproved,
                $"Your request for {gameName} {FormatRequestPeriod(request.StartDate, request.EndDate)} was approved.");

            return rentalId;
        }

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

            int rentalId;
            ImmutableList<Request> overlappingRequests;

            try
            {
                (rentalId, overlappingRequests) = _requestRepository.ApproveAtomically(
                    request, bufferedStart, bufferedEnd);
            }
            catch
            {
                return (int)ApproveOfferError.TRANSACTION_FAILED;
            }

            var ownerName = request.Owner?.DisplayName ?? "The owner";
            var renterName = request.Renter?.DisplayName ?? "The requester";
            var gameName = request.Game?.Name ?? "a game";

            NotifyOverlappingRequestsUnavailable(overlappingRequests, gameName);

            SendNotificationToUser(
                request.OfferingUser?.Id ?? request.Owner?.Id ?? MissingUserId,
                Constants.NotificationTitles.OfferAccepted,
                $"{renterName} accepted your offer for {gameName}",
                NotificationType.OfferResult);

            SendNotificationToUser(
                renterId,
                Constants.NotificationTitles.RentalConfirmed,
                $"You accepted the offer for {gameName} from {ownerName}",
                NotificationType.OfferResult);

            _notificationService.ScheduleUpcomingRentalReminder(
                request.Renter?.Id ?? MissingUserId,
                request.Owner?.Id ?? MissingUserId,
                request.Game?.Name ?? "your game",
                request.StartDate);
            return rentalId;
        }

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

            _requestRepository.UpdateStatus(requestId, RequestStatus.Open, null);
            _notificationService.DeleteNotificationsByRequestId(requestId);

            var renterName = request.Renter?.DisplayName ?? "The requester";
            var ownerName = request.Owner?.DisplayName ?? "The owner";
            var gameName = request.Game?.Name ?? "a game";

            SendNotificationToUser(
                request.OfferingUser?.Id ?? request.Owner?.Id ?? MissingUserId,
                Constants.NotificationTitles.OfferDenied,
                $"{renterName} denied your offer for {gameName}",
                NotificationType.OfferResult);

            SendNotificationToUser(
                renterId,
                Constants.NotificationTitles.OfferDeclined,
                $"You declined the offer for {gameName} from {ownerName}",
                NotificationType.OfferResult);

            return requestId;
        }

        private void NotifyOverlappingRequestsUnavailable(ImmutableList<Request> overlappingRequests, string gameName)
        {
            foreach (var overlappingRequest in overlappingRequests)
            {
                var renterId = overlappingRequest.Renter?.Id ?? MissingUserId;
                SendNotificationToUser(
                    renterId,
                    Constants.NotificationTitles.BookingUnavailable,
                    $"Your request for {gameName} {FormatRequestPeriod(overlappingRequest.StartDate, overlappingRequest.EndDate)} was declined because the game is no longer available in that period.");
            }
        }

        private void SendNotificationToUser(int userId, string title, string body, NotificationType type = default)
        {
            _notificationService.SendNotificationToUser(userId, BuildNotification(userId, title, body, type));
        }

        private NotificationDataTransferObject BuildNotification(int userId, string title, string body, NotificationType type)
        {
            return new NotificationDataTransferObject
            {
                Id = NewEntityId,
                User = new UserDataTransferObject { Id = userId },
                Timestamp = DateTime.UtcNow,
                Title = title,
                Body = body,
                Type = type
            };
        }

        private static string FormatRequestPeriod(DateTime startDate, DateTime endDate)
        {
            return $"({startDate:d}-{endDate:d})";
        }
    }
}
