using System.Collections.Immutable;
using System.Linq;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;

namespace Property_and_Management.Src.Viewmodels
{
    public class RequestsToOthersViewModel : PagedViewModel<RequestDataTransferObject>
    {
        private const int MinimumSuccessfulEntityIdentifier = 1;

        private readonly IRequestService requestService;
        private readonly ICurrentUserContext currentUserContext;

        public int RenterIdentifier { get; private set; }

        public RequestsToOthersViewModel(IRequestService requestService, ICurrentUserContext currentUserContext)
        {
            this.requestService = requestService;
            this.currentUserContext = currentUserContext;
            Reload();
        }

        public override string ShowingText => $"Showing {DisplayedCount} of {TotalCount} requests";

        public void LoadRequests() => Reload();

        protected override void Reload()
        {
            RenterIdentifier = currentUserContext.CurrentUserIdentifier;
            var allRequests = requestService
                .GetRequestsForRenter(RenterIdentifier)
                .OrderByDescending(request => request.StartDate)
                .ToImmutableList();
            SetAllItems(allRequests);
        }

        /// <summary>
        /// Attempt to cancel the given request on behalf of the current renter.
        /// Returns <c>null</c> on success, or a user-friendly error message on failure.
        /// Keeps the <c>Service</c> namespace out of the view layer.
        /// </summary>
        public string? TryCancelRequest(int requestIdentifier)
        {
            var rawResult = requestService.CancelRequest(requestIdentifier, RenterIdentifier);
            if (rawResult >= MinimumSuccessfulEntityIdentifier)
            {
                Reload();
                return null;
            }

            return ((CancelRequestError)rawResult) switch
            {
                CancelRequestError.NotFound => "Request not found.",
                CancelRequestError.Unauthorized => "You are not authorized to cancel this request.",
                _ => Constants.DialogMessages.UnexpectedErrorOccurred
            };
        }
    }
}
