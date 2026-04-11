using System.Collections.Immutable;
using System.Linq;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Viewmodels
{
    public class RequestsFromOthersViewModel : PagedViewModel<RequestDataTransferObject>
    {
        private readonly IRequestService requestService;
        private readonly ICurrentUserContext currentUserContext;

        public int OwnerIdentifier { get; private set; }

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
            OwnerIdentifier = currentUserContext.CurrentUserIdentifier;
            // Owners only see Open requests here. OfferPending requests have
            // already been offered to the renter and are awaiting their decision,
            // so showing an Offer button on them would just error out.
            var allRequests = requestService
                .GetRequestsForOwner(OwnerIdentifier)
                .Where(request => request.Status == RequestStatus.Open)
                .OrderByDescending(request => request.StartDate)
                .ToImmutableList();
            SetAllItems(allRequests);
        }

        /// <summary>
        /// Approve a pending request directly (creates a rental atomically).
        /// Returns null on success or a user-friendly error message on failure.
        /// </summary>
        public string? TryApproveRequest(int requestIdentifier)
        {
            var result = requestService.ApproveRequest(requestIdentifier, OwnerIdentifier);
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

        /// <summary>
        /// Decline a pending request. The view is free to pass a raw user string;
        /// we trim and substitute the "no reason provided" placeholder here so
        /// code-behind stays UI-only.
        /// </summary>
        public string? TryDenyRequest(int requestIdentifier, string? rawReason)
        {
            var trimmedReason = (rawReason ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmedReason))
            {
                trimmedReason = Constants.DialogMessages.NoReasonProvided;
            }

            var result = requestService.DenyRequest(requestIdentifier, OwnerIdentifier, trimmedReason);
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

        /// <summary>
        /// Offer the game to the renter. Flips the request into OfferPending and
        /// notifies the renter. Returns null on success or a user-friendly error.
        /// </summary>
        public string? TryOfferGame(int requestIdentifier)
        {
            var result = requestService.OfferGame(requestIdentifier, OwnerIdentifier);
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
                _ => Constants.DialogMessages.UnexpectedErrorOccurred
            };
        }
    }
}
