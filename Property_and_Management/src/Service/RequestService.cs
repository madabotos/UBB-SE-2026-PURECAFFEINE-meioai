using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Property_and_Management;
using Property_and_Management.Src.Constants;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Service
{
    /// <summary>
    /// Orchestrates the rental-request lifecycle: creation, approval, denial,
    /// offers and cancellations. Business decisions live here; repositories are
    /// only trusted with persistence primitives.
    /// </summary>
    public class RequestService : IRequestService
    {
        private const int NewEntityIdentifier = 0;
        private const int MissingUserIdentifier = 0;
        private const int MissingForeignKeyIdentifier = 0;
        private const int MissingOptionalDatePart = 0;
        private const int AvailabilityWindowMonths = 1;

        private readonly IRequestRepository requestRepository;
        private readonly IRentalRepository rentalRepository;
        private readonly INotificationService notificationService;
        private readonly IGameRepository gameRepository;
        private readonly IMapper<Request, RequestDataTransferObject> requestMapper;

        public RequestService(
            IRequestRepository requestRepository,
            IRentalRepository rentalRepository,
            IGameRepository gameRepository,
            INotificationService notificationService,
            IMapper<Request, RequestDataTransferObject> requestMapper)
        {
            this.requestRepository = requestRepository;
            this.rentalRepository = rentalRepository;
            this.gameRepository = gameRepository;
            this.notificationService = notificationService;
            this.requestMapper = requestMapper;
        }

        public ImmutableList<RequestDataTransferObject> GetRequestsForRenter(int renterIdentifier) =>
            requestRepository
                .GetRequestsByRenter(renterIdentifier)
                .Select(request => requestMapper.ToDataTransferObject(request))
                .ToImmutableList();

        public ImmutableList<RequestDataTransferObject> GetRequestsForOwner(int ownerIdentifier) =>
            requestRepository
                .GetRequestsByOwner(ownerIdentifier)
                .Select(request => requestMapper.ToDataTransferObject(request))
                .ToImmutableList();

        // [BL-LFC-01] Create a new pending request.
        public Result<int, CreateRequestError> CreateRequest(
            int gameIdentifier,
            int renterIdentifier,
            int ownerIdentifier,
            DateTime startDate,
            DateTime endDate)
        {
            if (renterIdentifier == ownerIdentifier)
            {
                return Result<int, CreateRequestError>.Failure(CreateRequestError.OwnerCannotRent);
            }

            try
            {
                gameRepository.Get(gameIdentifier);
            }
            catch (KeyNotFoundException)
            {
                return Result<int, CreateRequestError>.Failure(CreateRequestError.GameDoesNotExist);
            }

            if (!CheckAvailability(gameIdentifier, startDate, endDate))
            {
                return Result<int, CreateRequestError>.Failure(CreateRequestError.DatesUnavailable);
            }

            var request = new Request(
                identifier: NewEntityIdentifier,
                game: new Game { Identifier = gameIdentifier },
                renter: new User { Identifier = renterIdentifier },
                owner: new User { Identifier = ownerIdentifier },
                startDate: startDate,
                endDate: endDate);

            requestRepository.Add(request);
            return Result<int, CreateRequestError>.Success(request.Identifier);
        }

        // [BL-LFC-02] Owner directly accepts a request. The service owns the business
        // decision (find overlaps, notify cancelled renters, schedule reminders); the
        // repository only commits the transactional mechanics via ApproveAtomically.
        public Result<int, ApproveRequestError> ApproveRequest(int requestIdentifier, int ownerIdentifier)
        {
            Request request;
            try
            {
                request = requestRepository.Get(requestIdentifier);
            }
            catch (KeyNotFoundException)
            {
                return Result<int, ApproveRequestError>.Failure(ApproveRequestError.NotFound);
            }

            if (request.Owner?.Identifier != ownerIdentifier)
            {
                return Result<int, ApproveRequestError>.Failure(ApproveRequestError.Unauthorized);
            }

            // Only Open requests can be directly approved. OfferPending requests must go
            // through ApproveOffer so we never bypass the renter's consent.
            if (request.Status != RequestStatus.Open)
            {
                return Result<int, ApproveRequestError>.Failure(ApproveRequestError.NotFound);
            }

            var bufferedStart = request.StartDate.AddHours(-DomainConstants.RentalBufferHours);
            var bufferedEnd = request.EndDate.AddHours(DomainConstants.RentalBufferHours);

            var overlappingRequests = requestRepository.GetOverlappingRequests(
                request.Game?.Identifier ?? MissingForeignKeyIdentifier,
                request.Identifier,
                bufferedStart,
                bufferedEnd);

            int rentalIdentifier;
            try
            {
                rentalIdentifier = requestRepository.ApproveAtomically(request, overlappingRequests);
            }
            catch
            {
                return Result<int, ApproveRequestError>.Failure(ApproveRequestError.TransactionFailed);
            }

            var gameName = request.Game?.Name ?? "the selected game";
            NotifyOverlappingRequestsUnavailable(overlappingRequests, gameName);

            SendNotificationToUser(
                request.Renter?.Identifier ?? MissingUserIdentifier,
                Constants.NotificationTitles.RentalRequestApproved,
                $"Your request for {gameName} {FormatRequestPeriod(request.StartDate, request.EndDate)} was approved.");

            notificationService.ScheduleUpcomingRentalReminder(
                request.Renter?.Identifier ?? MissingUserIdentifier,
                request.Owner?.Identifier ?? MissingUserIdentifier,
                request.Game?.Name ?? "your game",
                request.StartDate);

            return Result<int, ApproveRequestError>.Success(rentalIdentifier);
        }

        // [BL-LFC-03] Owner declines a request.
        public Result<int, DenyRequestError> DenyRequest(int requestIdentifier, int ownerIdentifier, string reason)
        {
            Request request;
            try
            {
                request = requestRepository.Get(requestIdentifier);
            }
            catch (KeyNotFoundException)
            {
                return Result<int, DenyRequestError>.Failure(DenyRequestError.NotFound);
            }

            if (request.Owner?.Identifier != ownerIdentifier)
            {
                return Result<int, DenyRequestError>.Failure(DenyRequestError.Unauthorized);
            }

            notificationService.DeleteNotificationsByRequestId(requestIdentifier);
            requestRepository.Delete(requestIdentifier);

            var renterIdentifier = request.Renter?.Identifier ?? MissingUserIdentifier;
            var gameName = request.Game?.Name ?? "the selected game";
            SendNotificationToUser(
                renterIdentifier,
                Constants.NotificationTitles.RentalRequestDeclined,
                $"Your request for {gameName} {FormatRequestPeriod(request.StartDate, request.EndDate)} was declined. Reason: {reason}");

            return Result<int, DenyRequestError>.Success(requestIdentifier);
        }

        // [BL-LFC-04] Renter cancels their own pending request. Keeps the legacy int
        // return shape: positive = cancelled request id, negative = CancelRequestError.
        public int CancelRequest(int requestIdentifier, int cancellingUserIdentifier)
        {
            Request request;
            try
            {
                request = requestRepository.Get(requestIdentifier);
            }
            catch (KeyNotFoundException)
            {
                return (int)CancelRequestError.NotFound;
            }

            if (request.Renter?.Identifier != cancellingUserIdentifier)
            {
                return (int)CancelRequestError.Unauthorized;
            }

            notificationService.DeleteNotificationsByRequestId(requestIdentifier);
            try
            {
                requestRepository.Delete(requestIdentifier);
            }
            catch (KeyNotFoundException)
            {
                return (int)CancelRequestError.NotFound;
            }

            return requestIdentifier;
        }

        // [BL-LFC-05] Game deactivated — decline all pending requests for it.
        public void OnGameDeactivated(int gameIdentifier)
        {
            var pending = requestRepository
                .GetRequestsByGame(gameIdentifier)
                .Where(request => request.Status == RequestStatus.Open || request.Status == RequestStatus.OfferPending)
                .ToImmutableList();

            foreach (var request in pending)
            {
                notificationService.DeleteNotificationsByRequestId(request.Identifier);
                requestRepository.Delete(request.Identifier);

                var renterIdentifier = request.Renter?.Identifier ?? MissingUserIdentifier;
                var gameName = request.Game?.Name ?? "the selected game";
                SendNotificationToUser(
                    renterIdentifier,
                    Constants.NotificationTitles.RentalRequestCancelled,
                    $"Your request for {gameName} {FormatRequestPeriod(request.StartDate, request.EndDate)} has been cancelled because the game is no longer available.");
            }
        }

        public ImmutableList<(DateTime StartDate, DateTime EndDate)> GetBookedDates(
            int gameIdentifier,
            int month = MissingOptionalDatePart,
            int year = MissingOptionalDatePart)
        {
            if (month == MissingOptionalDatePart)
            {
                month = DateTime.UtcNow.Month;
            }

            if (year == MissingOptionalDatePart)
            {
                year = DateTime.UtcNow.Year;
            }

            return requestRepository
                .GetRequestsByGame(gameIdentifier)
                .Where(request => request.StartDate.Month == month && request.StartDate.Year == year)
                .OrderBy(request => request.StartDate)
                .Select(request => (request.StartDate, request.EndDate.AddHours(DomainConstants.RentalBufferHours)))
                .ToImmutableList();
        }

        public bool CheckAvailability(int gameIdentifier, DateTime startDate, DateTime endDate)
        {
            var oneMonthFromNow = DateTime.UtcNow.AddMonths(AvailabilityWindowMonths);
            if (startDate > oneMonthFromNow || endDate > oneMonthFromNow)
            {
                return false;
            }

            Game game;
            try
            {
                game = gameRepository.Get(gameIdentifier);
            }
            catch (KeyNotFoundException)
            {
                return false;
            }

            if (!game.IsActive)
            {
                return false;
            }

            // Apply the 48h buffer symmetrically on both sides of existing rentals, matching
            // RentalService.IsSlotAvailable and RentalRepository.IsSlotAvailableInternal.
            bool rentalConflict = rentalRepository
                .GetRentalsByGame(gameIdentifier)
                .Any(rental => startDate < rental.EndDate.AddHours(DomainConstants.RentalBufferHours) &&
                               endDate > rental.StartDate.AddHours(-DomainConstants.RentalBufferHours));

            if (rentalConflict)
            {
                return false;
            }

            bool requestConflict = requestRepository
                .GetRequestsByGame(gameIdentifier)
                .Any(request => request.StartDate.AddHours(-DomainConstants.RentalBufferHours) < endDate &&
                                request.EndDate.AddHours(DomainConstants.RentalBufferHours) > startDate);

            return !requestConflict;
        }

        // [BL-OFR-01] Owner offers their game for a pending request. This is NOT an
        // immediate rental — it flips the request into OfferPending status and sends an
        // actionable OfferReceived notification to the renter, who can then approve it
        // (creating the rental via ApproveOffer) or deny it (reverting to Open).
        public Result<int, OfferError> OfferGame(int requestIdentifier, int offeringUserIdentifier)
        {
            Request request;
            try
            {
                request = requestRepository.Get(requestIdentifier);
            }
            catch (KeyNotFoundException)
            {
                return Result<int, OfferError>.Failure(OfferError.NotFound);
            }

            if (request.Owner?.Identifier != offeringUserIdentifier)
            {
                return Result<int, OfferError>.Failure(OfferError.NotOwner);
            }

            if (request.Status != RequestStatus.Open)
            {
                return Result<int, OfferError>.Failure(OfferError.RequestNotOpen);
            }

            requestRepository.UpdateStatus(requestIdentifier, RequestStatus.OfferPending, offeringUserIdentifier);

            var renterIdentifier = request.Renter?.Identifier ?? MissingUserIdentifier;
            var ownerName = request.Owner?.DisplayName ?? "The owner";
            var gameName = request.Game?.Name ?? "the selected game";

            SendNotificationToUser(
                renterIdentifier,
                Constants.NotificationTitles.OfferReceived,
                $"{ownerName} is offering you {gameName} {FormatRequestPeriod(request.StartDate, request.EndDate)}. Open Notifications to approve or deny.",
                NotificationType.OfferReceived,
                requestIdentifier);

            return Result<int, OfferError>.Success(requestIdentifier);
        }

        // [BL-OFR-02] Renter approves a pending offer — mirrors ApproveRequest but gated
        // on the renter's identity instead of the owner's.
        public Result<int, ApproveOfferError> ApproveOffer(int requestIdentifier, int renterIdentifier)
        {
            Request request;
            try
            {
                request = requestRepository.Get(requestIdentifier);
            }
            catch (KeyNotFoundException)
            {
                return Result<int, ApproveOfferError>.Failure(ApproveOfferError.NotFound);
            }

            if (request.Renter?.Identifier != renterIdentifier)
            {
                return Result<int, ApproveOfferError>.Failure(ApproveOfferError.NotRenter);
            }

            if (request.Status != RequestStatus.OfferPending)
            {
                return Result<int, ApproveOfferError>.Failure(ApproveOfferError.NoPendingOffer);
            }

            var bufferedStart = request.StartDate.AddHours(-DomainConstants.RentalBufferHours);
            var bufferedEnd = request.EndDate.AddHours(DomainConstants.RentalBufferHours);

            var overlappingRequests = requestRepository.GetOverlappingRequests(
                request.Game?.Identifier ?? MissingForeignKeyIdentifier,
                request.Identifier,
                bufferedStart,
                bufferedEnd);

            int rentalIdentifier;
            try
            {
                rentalIdentifier = requestRepository.ApproveAtomically(request, overlappingRequests);
            }
            catch
            {
                return Result<int, ApproveOfferError>.Failure(ApproveOfferError.TransactionFailed);
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

            notificationService.ScheduleUpcomingRentalReminder(
                request.Renter?.Identifier ?? MissingUserIdentifier,
                request.Owner?.Identifier ?? MissingUserIdentifier,
                request.Game?.Name ?? "your game",
                request.StartDate);

            return Result<int, ApproveOfferError>.Success(rentalIdentifier);
        }

        // [BL-OFR-03] Renter denies a pending offer — status reverts to Open and both
        // parties are notified.
        public Result<int, DenyOfferError> DenyOffer(int requestIdentifier, int renterIdentifier)
        {
            Request request;
            try
            {
                request = requestRepository.Get(requestIdentifier);
            }
            catch (KeyNotFoundException)
            {
                return Result<int, DenyOfferError>.Failure(DenyOfferError.NotFound);
            }

            if (request.Renter?.Identifier != renterIdentifier)
            {
                return Result<int, DenyOfferError>.Failure(DenyOfferError.NotRenter);
            }

            if (request.Status != RequestStatus.OfferPending)
            {
                return Result<int, DenyOfferError>.Failure(DenyOfferError.NoPendingOffer);
            }

            requestRepository.UpdateStatus(requestIdentifier, RequestStatus.Open, null);
            notificationService.DeleteNotificationsByRequestId(requestIdentifier);

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

            return Result<int, DenyOfferError>.Success(requestIdentifier);
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

        private void SendNotificationToUser(
            int userIdentifier,
            string title,
            string body,
            NotificationType type = default,
            int? relatedRequestIdentifier = null)
        {
            notificationService.SendNotificationToUser(
                userIdentifier,
                BuildNotification(userIdentifier, title, body, type, relatedRequestIdentifier));
        }

        private NotificationDataTransferObject BuildNotification(
            int userIdentifier,
            string title,
            string body,
            NotificationType type,
            int? relatedRequestIdentifier)
        {
            return new NotificationDataTransferObject
            {
                Identifier = NewEntityIdentifier,
                User = new UserDataTransferObject { Identifier = userIdentifier },
                Timestamp = DateTime.UtcNow,
                Title = title,
                Body = body,
                Type = type,
                RelatedRequestIdentifier = relatedRequestIdentifier
            };
        }

        private static string FormatRequestPeriod(DateTime startDate, DateTime endDate)
        {
            return $"({startDate:d}-{endDate:d})";
        }
    }
}
