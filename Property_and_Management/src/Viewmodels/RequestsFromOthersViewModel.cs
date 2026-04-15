using System.Collections.Immutable;
using System.Linq;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Viewmodels
{
    public class RequestsFromOthersViewModel : PagedViewModel<RequestDTO>
    {
        private readonly IRequestService requestService;
        private readonly ICurrentUserContext currentUserContext;

        public int ownerId { get; private set; }

        public RequestsFromOthersViewModel(IRequestService requestService, ICurrentUserContext currentUserContext)
        {
            this.requestService = requestService;
            this.currentUserContext = currentUserContext;
            Reload();
        }

        public override string ShowingText => $"Showing {DisplayedCount} of {TotalCount} requests";

        public void LoadRequests() => Reload();

        protected override void Reload()
        {
            ownerId = currentUserContext.currentUserId;

            var allRequests = requestService
                .GetRequestsForOwner(ownerId)
                .Where(request => request.Status == RequestStatus.Open)
                .OrderByDescending(request => request.StartDate)
                .ToImmutableList();
            SetAllItems(allRequests);
        }

        public string? TryApproveRequest(int requestId)
        {
            var result = requestService.ApproveRequest(requestId, ownerId);
            if (result.IsSuccess)
            {
                Reload();
                return null;
            }

            return result.Error switch
            {
                ApproveRequestError.Unauthorized => "You are not authorized to approve this request.",
                ApproveRequestError.NotFound => "Request not found.",
                ApproveRequestError.TransactionFailed => "Could not approve the request. Please try again.",
                _ => Constants.DialogMessages.UnexpectedErrorOccurred
            };
        }

        public string? TryDenyRequest(int requestId, string? rawReason)
        {
            var trimmedReason = (rawReason ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmedReason))
            {
                trimmedReason = Constants.DialogMessages.NoReasonProvided;
            }

            var result = requestService.DenyRequest(requestId, ownerId, trimmedReason);
            if (result.IsSuccess)
            {
                Reload();
                return null;
            }

            return result.Error switch
            {
                DenyRequestError.NotFound => "Request not found.",
                DenyRequestError.Unauthorized => "You are not authorized to deny this request.",
                _ => Constants.DialogMessages.UnexpectedErrorOccurred
            };
        }

        public string? TryOfferGame(int requestId)
        {
            var result = requestService.OfferGame(requestId, ownerId);
            if (result.IsSuccess)
            {
                Reload();
                return null;
            }

            return result.Error switch
            {
                OfferError.NotFound => "Request not found.",
                OfferError.NotOwner => "You are not the owner of this game.",
                OfferError.RequestNotOpen => "This request is no longer open.",
                OfferError.TransactionFailed => "Could not approve the request. Please try again.",
                _ => Constants.DialogMessages.UnexpectedErrorOccurred
            };
        }
    }
}