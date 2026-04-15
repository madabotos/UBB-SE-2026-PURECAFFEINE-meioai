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
    public class RequestService : IRequestService
    {
        private const int NewEntityId = 0;
        private const int MissingUserId = 0;
        private const int MissingForeignKeyId = 0;
        private const int MissingOptionalDatePart = 0;
        private const int AvailabilityWindowMonths = 1;

        private readonly IRequestRepository requestRepository;
        private readonly IRentalRepository rentalRepository;
        private readonly INotificationService notificationService;
        private readonly IGameRepository gameRepository;
        private readonly IMapper<Request, RequestDTO> requestMapper;

        public RequestService(
            IRequestRepository requestRepository,
            IRentalRepository rentalRepository,
            IGameRepository gameRepository,
            INotificationService notificationService,
            IMapper<Request, RequestDTO> requestMapper)
        {
            this.requestRepository = requestRepository;
            this.rentalRepository = rentalRepository;
            this.gameRepository = gameRepository;
            this.notificationService = notificationService;
            this.requestMapper = requestMapper;
        }

        public ImmutableList<RequestDTO> GetRequestsForRenter(int renterId) =>
            requestRepository
                .GetRequestsByRenter(renterId)
                .Select(request => requestMapper.ToDTO(request))
                .ToImmutableList();

        public ImmutableList<RequestDTO> GetRequestsForOwner(int ownerId) =>
            requestRepository
                .GetRequestsByOwner(ownerId)
                .Select(request => requestMapper.ToDTO(request))
                .ToImmutableList();

        public Result<int, CreateRequestError> CreateRequest(
            int gameId,
            int renterId,
            int ownerId,
            DateTime startDate,
            DateTime endDate)
        {
            if (renterId == ownerId)
            {
                return Result<int, CreateRequestError>.Failure(CreateRequestError.OwnerCannotRent);
            }

            try
            {
                gameRepository.Get(gameId);
            }
            catch (KeyNotFoundException)
            {
                return Result<int, CreateRequestError>.Failure(CreateRequestError.GameDoesNotExist);
            }

            if (!CheckAvailability(gameId, startDate, endDate))
            {
                return Result<int, CreateRequestError>.Failure(CreateRequestError.DatesUnavailable);
            }

            var request = new Request(
                id: NewEntityId,
                game: new Game { Id = gameId },
                renter: new User { Id = renterId },
                owner: new User { Id = ownerId },
                startDate: startDate,
                endDate: endDate);

            requestRepository.Add(request);
            return Result<int, CreateRequestError>.Success(request.Id);
        }

        public Result<int, ApproveRequestError> ApproveRequest(int requestId, int ownerId)
        {
            Request request;
            try
            {
                request = requestRepository.Get(requestId);
            }
            catch (KeyNotFoundException)
            {
                return Result<int, ApproveRequestError>.Failure(ApproveRequestError.NotFound);
            }

            if (request.Owner?.Id != ownerId)
            {
                return Result<int, ApproveRequestError>.Failure(ApproveRequestError.Unauthorized);
            }

            if (request.Status != RequestStatus.Open)
            {
                return Result<int, ApproveRequestError>.Failure(ApproveRequestError.NotFound);
            }

            if (!TryApproveOpenRequestAndNotify(request, out var rentalId))
            {
                return Result<int, ApproveRequestError>.Failure(ApproveRequestError.TransactionFailed);
            }

            return Result<int, ApproveRequestError>.Success(rentalId);
        }

        public Result<int, DenyRequestError> DenyRequest(int requestId, int ownerId, string reason)
        {
            Request request;
            try
            {
                request = requestRepository.Get(requestId);
            }
            catch (KeyNotFoundException)
            {
                return Result<int, DenyRequestError>.Failure(DenyRequestError.NotFound);
            }

            if (request.Owner?.Id != ownerId)
            {
                return Result<int, DenyRequestError>.Failure(DenyRequestError.Unauthorized);
            }

            notificationService.DeleteNotificationsLinkedToRequest(requestId);
            requestRepository.Delete(requestId);

            var renterId = request.Renter?.Id ?? MissingUserId;
            var gameName = request.Game?.Name ?? "the selected game";
            SendNotificationToUser(
                renterId,
                Constants.NotificationTitles.RentalRequestDeclined,
                $"Your request for {gameName} {FormatRequestPeriod(request.StartDate, request.EndDate)} was declined. Reason: {reason}");

            return Result<int, DenyRequestError>.Success(requestId);
        }

        public int CancelRequest(int requestId, int cancellingUserId)
        {
            Request request;
            try
            {
                request = requestRepository.Get(requestId);
            }
            catch (KeyNotFoundException)
            {
                return (int)CancelRequestError.NotFound;
            }

            if (request.Renter?.Id != cancellingUserId)
            {
                return (int)CancelRequestError.Unauthorized;
            }

            notificationService.DeleteNotificationsLinkedToRequest(requestId);
            try
            {
                requestRepository.Delete(requestId);
            }
            catch (KeyNotFoundException)
            {
                return (int)CancelRequestError.NotFound;
            }

            return requestId;
        }

        public void OnGameDeactivated(int gameId)
        {
            var pending = requestRepository
                .GetRequestsByGame(gameId)
                .Where(IsPendingForGameDeactivation)
                .ToImmutableList();

            foreach (var request in pending)
            {
                notificationService.DeleteNotificationsLinkedToRequest(request.Id);
                requestRepository.Delete(request.Id);

                var renterId = request.Renter?.Id ?? MissingUserId;
                var gameName = request.Game?.Name ?? "the selected game";
                SendNotificationToUser(
                    renterId,
                    Constants.NotificationTitles.RentalRequestCancelled,
                    $"Your request for {gameName} {FormatRequestPeriod(request.StartDate, request.EndDate)} has been cancelled because the game is no longer available.");
            }
        }

        private static bool IsPendingForGameDeactivation(Request request)
        {
            return request.Status == RequestStatus.Open ||
                   request.Status == RequestStatus.OfferPending;
        }

        public ImmutableList<(DateTime StartDate, DateTime EndDate)> GetBookedDates(
            int gameId,
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
                .GetRequestsByGame(gameId)
                .Where(request => request.StartDate.Month == month && request.StartDate.Year == year)
                .OrderBy(request => request.StartDate)
                .Select(request => (request.StartDate, request.EndDate.AddHours(DomainConstants.RentalBufferHours)))
                .ToImmutableList();
        }

        public bool CheckAvailability(int gameId, DateTime startDate, DateTime endDate)
        {
            var oneMonthFromNow = DateTime.UtcNow.AddMonths(AvailabilityWindowMonths);
            if (startDate > oneMonthFromNow || endDate > oneMonthFromNow)
            {
                return false;
            }

            Game game;
            try
            {
                game = gameRepository.Get(gameId);
            }
            catch (KeyNotFoundException)
            {
                return false;
            }

            if (!game.IsActive)
            {
                return false;
            }

            bool rentalConflict = rentalRepository
                .GetRentalsByGame(gameId)
                .Any(rental => startDate < rental.EndDate.AddHours(DomainConstants.RentalBufferHours) &&
                               endDate > rental.StartDate.AddHours(-DomainConstants.RentalBufferHours));

            if (rentalConflict)
            {
                return false;
            }

            bool requestConflict = requestRepository
                .GetRequestsByGame(gameId)
                .Any(request => request.StartDate.AddHours(-DomainConstants.RentalBufferHours) < endDate &&
                                request.EndDate.AddHours(DomainConstants.RentalBufferHours) > startDate);

            return !requestConflict;
        }

        public Result<int, OfferError> OfferGame(int requestId, int offeringUserId)
        {
            Request request;
            try
            {
                request = requestRepository.Get(requestId);
            }
            catch (KeyNotFoundException)
            {
                return Result<int, OfferError>.Failure(OfferError.NotFound);
            }

            if (request.Owner?.Id != offeringUserId)
            {
                return Result<int, OfferError>.Failure(OfferError.NotOwner);
            }

            if (request.Status != RequestStatus.Open)
            {
                return Result<int, OfferError>.Failure(OfferError.RequestNotOpen);
            }

            if (!TryApproveOpenRequestAndNotify(request, out var rentalId))
            {
                return Result<int, OfferError>.Failure(OfferError.TransactionFailed);
            }

            return Result<int, OfferError>.Success(rentalId);
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

        private void SendNotificationToUser(
            int userId,
            string title,
            string body,
            NotificationType type = default,
            int? relatedRequestId = null)
        {
            notificationService.SendNotificationToUser(
                userId,
                BuildNotification(userId, title, body, type, relatedRequestId));
        }

        private NotificationDTO BuildNotification(
            int userId,
            string title,
            string body,
                NotificationType type,
                int? relatedRequestId)
        {
            return new NotificationDTO
            {
                Id = NewEntityId,
                User = new UserDTO { Id = userId },
                Timestamp = DateTime.UtcNow,
                Title = title,
                Body = body,
                Type = type,
                RelatedRequestId = relatedRequestId
            };
        }

        private bool TryApproveOpenRequestAndNotify(Request request, out int rentalId)
        {
            var bufferedStartDate = request.StartDate.AddHours(-DomainConstants.RentalBufferHours);
            var bufferedEndDate = request.EndDate.AddHours(DomainConstants.RentalBufferHours);

            var overlappingRequests = requestRepository.GetOverlappingRequests(
                request.Game?.Id ?? MissingForeignKeyId,
                request.Id,
                bufferedStartDate,
                bufferedEndDate);

            try
            {
                rentalId = requestRepository.ApproveAtomically(request, overlappingRequests);
            }
            catch
            {
                rentalId = MissingForeignKeyId;
                return false;
            }

            var gameName = request.Game?.Name ?? "the selected game";
            NotifyOverlappingRequestsUnavailable(overlappingRequests, gameName);

            SendNotificationToUser(
                request.Renter?.Id ?? MissingUserId,
                Constants.NotificationTitles.RentalRequestApproved,
                $"Your request for {gameName} {FormatRequestPeriod(request.StartDate, request.EndDate)} was approved.");

            notificationService.ScheduleUpcomingRentalReminder(
                request.Renter?.Id ?? MissingUserId,
                request.Owner?.Id ?? MissingUserId,
                request.Game?.Name ?? "your game",
                request.StartDate);

            return true;
        }

        private static string FormatRequestPeriod(DateTime startDate, DateTime endDate)
        {
            return $"({startDate:d}-{endDate:d})";
        }
    }
}
