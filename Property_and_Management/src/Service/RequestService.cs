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
        private const int NewEntityIdentifier = 0;
        private const int MissingUserIdentifier = 0;
        private const int MissingOptionalDatePart = 0;
        private const int AvailabilityWindowMonths = 1;
        private const int MinimumSuccessfulEntityIdentifier = 1;

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

        public ImmutableList<RequestDataTransferObject> GetRequestsForRenter(int renterIdentifier) =>
            _requestRepository
                .GetRequestsByRenter(renterIdentifier)
                .Select(request => _requestMapper.ToDataTransferObject(request))
                .ToImmutableList();

        public ImmutableList<RequestDataTransferObject> GetRequestsForOwner(int ownerIdentifier) =>
            _requestRepository
                .GetRequestsByOwner(ownerIdentifier)
                .Select(request => _requestMapper.ToDataTransferObject(request))
                .ToImmutableList();

        // [BL-LFC-01] Create a new PENDING request
        public int CreateRequest(int gameIdentifier, int renterIdentifier, int ownerIdentifier,
                                 DateTime startDate, DateTime endDate)
        {
            if (renterIdentifier == ownerIdentifier)
                return (int)CreateRequestError.OWNER_CANNOT_RENT_ERROR;

            try { _gameRepository.Get(gameIdentifier); }
            catch (KeyNotFoundException)
            { return (int)CreateRequestError.GAME_ID_DOES_NOT_EXIST_ERROR; }

            if (!CheckAvailability(gameIdentifier, startDate, endDate))
                return (int)CreateRequestError.DATES_UNAVAILABLE_ERROR;

            var request = new Request(
                identifier: NewEntityIdentifier,
                game: new Game { Identifier = gameIdentifier },
                renter: new User { Identifier = renterIdentifier },
                owner: new User { Identifier = ownerIdentifier },
                startDate: startDate,
                endDate: endDate);

            _requestRepository.Add(request);
            return request.Identifier;
        }

        // [BL-LFC-02] Owner accepts a request — fully atomic via repository
        public int ApproveRequest(int requestIdentifier, int ownerIdentifier)
        {
            Request request;
            try { request = _requestRepository.Get(requestIdentifier); }
            catch (KeyNotFoundException)
            { return (int)ApproveRequestError.NOT_FOUND_ERROR; }

            if (request.Owner?.Identifier != ownerIdentifier)
                return (int)ApproveRequestError.UNAUTHORIZED_ERROR;

            var bufferedStart = request.StartDate.AddHours(-BufferHours);
            var bufferedEnd = request.EndDate.AddHours(BufferHours);

            int rentalIdentifier;
            ImmutableList<Request> overlappingRequests;

            try
            {
                (rentalIdentifier, overlappingRequests) = _requestRepository.ApproveAtomically(
                    request, bufferedStart, bufferedEnd);
            }
            catch
            {
                return (int)ApproveRequestError.TRANSACTION_FAILED_ERROR;
            }

            NotifyOverlappingRequestsUnavailable(overlappingRequests, request.Game?.Name ?? "the selected game");

            _notificationService.ScheduleUpcomingRentalReminder(
                request.Renter?.Identifier ?? MissingUserIdentifier,
                request.Owner?.Identifier ?? MissingUserIdentifier,
                request.Game?.Name ?? "your game",
                request.StartDate);
            return rentalIdentifier;
        }

        // [BL-LFC-03] Owner declines a request
        public int DenyRequest(int requestIdentifier, int ownerIdentifier, string reason)
        {
            Request request;
            try { request = _requestRepository.Get(requestIdentifier); }
            catch (KeyNotFoundException)
            { return (int)DenyRequestError.NOT_FOUND_ERROR; }

            if (request.Owner?.Identifier != ownerIdentifier)
                return (int)DenyRequestError.UNAUTHORIZED_ERROR;

            _notificationService.DeleteNotificationsByRequestId(requestIdentifier);
            _requestRepository.Delete(requestIdentifier);

            var renterIdentifier = request.Renter?.Identifier ?? MissingUserIdentifier;
            var gameName = request.Game?.Name ?? "the selected game";
            SendNotificationToUser(
                renterIdentifier,
                Constants.NotificationTitles.RentalRequestDeclined,
                $"Your request for {gameName} {FormatRequestPeriod(request.StartDate, request.EndDate)} was declined. Reason: {reason}");

            return requestIdentifier;
        }

        // [BL-LFC-04] Renter cancels their own pending request
        public void CancelRequest(int requestIdentifier)
        {
            try
            {
                _notificationService.DeleteNotificationsByRequestId(requestIdentifier);
                _requestRepository.Delete(requestIdentifier);
            }
            catch (KeyNotFoundException) { }
        }

        // [BL-LFC-05] Game deactivated — decline all pending requests
        public void OnGameDeactivated(int gameIdentifier)
        {
            var pending = _requestRepository
                .GetRequestsByGame(gameIdentifier)
                .Where(request => request.Status == RequestStatus.Open || request.Status == RequestStatus.OfferPending)
                .ToImmutableList();

            foreach (var request in pending)
            {
                _notificationService.DeleteNotificationsByRequestId(request.Identifier);
                _requestRepository.Delete(request.Identifier);

                var renterIdentifier = request.Renter?.Identifier ?? MissingUserIdentifier;
                var gameName = request.Game?.Name ?? "the selected game";
                SendNotificationToUser(
                    renterIdentifier,
                    Constants.NotificationTitles.RentalRequestCancelled,
                    $"Your request for {gameName} {FormatRequestPeriod(request.StartDate, request.EndDate)} has been cancelled because the game is no longer available.");
            }
        }

        public ImmutableList<(DateTime, DateTime)> GetBookedDates(
            int gameIdentifier,
            int month = MissingOptionalDatePart,
            int year = MissingOptionalDatePart)
        {
            if (month == MissingOptionalDatePart) month = DateTime.UtcNow.Month;
            if (year == MissingOptionalDatePart) year = DateTime.UtcNow.Year;

            return _requestRepository
                .GetRequestsByGame(gameIdentifier)
                .Where(request => request.StartDate.Month == month && request.StartDate.Year == year)
                .OrderBy(request => request.StartDate)
                .Select(request => (request.StartDate, request.EndDate.AddHours(BufferHours)))
                .ToImmutableList();
        }

        public bool CheckAvailability(int gameIdentifier, DateTime startDate, DateTime endDate)
        {
            var oneMonthFromNow = DateTime.UtcNow.AddMonths(AvailabilityWindowMonths);
            if (startDate > oneMonthFromNow || endDate > oneMonthFromNow)
                return false;

            Game game;
            try { game = _gameRepository.Get(gameIdentifier); }
            catch (KeyNotFoundException) { return false; }

            if (!game.IsActive) return false;

            bool rentalConflict = _rentalRepository
                .GetRentalsByGame(gameIdentifier)
                .Any(rental => startDate < rental.EndDate.AddHours(BufferHours) &&
                               endDate > rental.StartDate);

            if (rentalConflict) return false;

            bool requestConflict = _requestRepository
                .GetRequestsByGame(gameIdentifier)
                .Any(request => request.StartDate < endDate.AddHours(BufferHours) &&
                                request.EndDate.AddHours(BufferHours) > startDate);

            return !requestConflict;
        }

        public int OfferGame(int requestIdentifier, int offeringUserIdentifier)
        {
            Request request;
            try { request = _requestRepository.Get(requestIdentifier); }
            catch (KeyNotFoundException)
            { return (int)OfferError.NOT_FOUND; }

            if (request.Owner?.Identifier != offeringUserIdentifier)
                return (int)OfferError.NOT_OWNER;

            if (request.Status != RequestStatus.Open)
                return (int)OfferError.REQUEST_NOT_OPEN;

            var rentalIdentifier = ApproveRequest(requestIdentifier, offeringUserIdentifier);
            if (rentalIdentifier < MinimumSuccessfulEntityIdentifier)
                return rentalIdentifier;

            var renterIdentifier = request.Renter?.Identifier ?? MissingUserIdentifier;
            var gameName = request.Game?.Name ?? "the selected game";
            SendNotificationToUser(
                renterIdentifier,
                Constants.NotificationTitles.RentalRequestApproved,
                $"Your request for {gameName} {FormatRequestPeriod(request.StartDate, request.EndDate)} was approved.");

            return rentalIdentifier;
        }

        public int ApproveOffer(int requestIdentifier, int renterIdentifier)
        {
            Request request;
            try { request = _requestRepository.Get(requestIdentifier); }
            catch (KeyNotFoundException)
            { return (int)ApproveOfferError.NOT_FOUND; }

            if (request.Renter?.Identifier != renterIdentifier)
                return (int)ApproveOfferError.NOT_RENTER;

            if (request.Status != RequestStatus.OfferPending)
                return (int)ApproveOfferError.NO_PENDING_OFFER;

            var bufferedStart = request.StartDate.AddHours(-BufferHours);
            var bufferedEnd = request.EndDate.AddHours(BufferHours);

            int rentalIdentifier;
            ImmutableList<Request> overlappingRequests;

            try
            {
                (rentalIdentifier, overlappingRequests) = _requestRepository.ApproveAtomically(
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
                request.OfferingUser?.Identifier ?? request.Owner?.Identifier ?? MissingUserIdentifier,
                Constants.NotificationTitles.OfferAccepted,
                $"{renterName} accepted your offer for {gameName}",
                NotificationType.OfferResult);

            SendNotificationToUser(
                renterIdentifier,
                Constants.NotificationTitles.RentalConfirmed,
                $"You accepted the offer for {gameName} from {ownerName}",
                NotificationType.OfferResult);

            _notificationService.ScheduleUpcomingRentalReminder(
                request.Renter?.Identifier ?? MissingUserIdentifier,
                request.Owner?.Identifier ?? MissingUserIdentifier,
                request.Game?.Name ?? "your game",
                request.StartDate);
            return rentalIdentifier;
        }

        public int DenyOffer(int requestIdentifier, int renterIdentifier)
        {
            Request request;
            try { request = _requestRepository.Get(requestIdentifier); }
            catch (KeyNotFoundException)
            { return (int)DenyOfferError.NOT_FOUND; }

            if (request.Renter?.Identifier != renterIdentifier)
                return (int)DenyOfferError.NOT_RENTER;

            if (request.Status != RequestStatus.OfferPending)
                return (int)DenyOfferError.NO_PENDING_OFFER;

            _requestRepository.UpdateStatus(requestIdentifier, RequestStatus.Open, null);
            _notificationService.DeleteNotificationsByRequestId(requestIdentifier);

            var renterName = request.Renter?.DisplayName ?? "The requester";
            var ownerName = request.Owner?.DisplayName ?? "The owner";
            var gameName = request.Game?.Name ?? "a game";

            SendNotificationToUser(
                request.OfferingUser?.Identifier ?? request.Owner?.Identifier ?? MissingUserIdentifier,
                Constants.NotificationTitles.OfferDenied,
                $"{renterName} denied your offer for {gameName}",
                NotificationType.OfferResult);

            SendNotificationToUser(
                renterIdentifier,
                Constants.NotificationTitles.OfferDeclined,
                $"You declined the offer for {gameName} from {ownerName}",
                NotificationType.OfferResult);

            return requestIdentifier;
        }

        private void NotifyOverlappingRequestsUnavailable(ImmutableList<Request> overlappingRequests, string gameName)
        {
            foreach (var overlappingRequest in overlappingRequests)
            {
                var renterIdentifier = overlappingRequest.Renter?.Identifier ?? MissingUserIdentifier;
                SendNotificationToUser(
                    renterIdentifier,
                    Constants.NotificationTitles.BookingUnavailable,
                    $"Your request for {gameName} {FormatRequestPeriod(overlappingRequest.StartDate, overlappingRequest.EndDate)} was declined because the game is no longer available in that period.");
            }
        }

        private void SendNotificationToUser(int userIdentifier, string title, string body, NotificationType type = default)
        {
            _notificationService.SendNotificationToUser(userIdentifier, BuildNotification(userIdentifier, title, body, type));
        }

        private NotificationDataTransferObject BuildNotification(int userIdentifier, string title, string body, NotificationType type)
        {
            return new NotificationDataTransferObject
            {
                Identifier = NewEntityIdentifier,
                User = new UserDataTransferObject { Identifier = userIdentifier },
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



