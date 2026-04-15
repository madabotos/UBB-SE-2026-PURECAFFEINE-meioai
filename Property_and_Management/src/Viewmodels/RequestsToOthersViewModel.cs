using System.Collections.Immutable;
using System.Linq;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;

namespace Property_and_Management.Src.Viewmodels
{
    public class RequestsToOthersViewModel : PagedViewModel<RequestDTO>
    {
        private const int MinimumSuccessfulEntityId = 1;

        private readonly IRequestService requestService;
        private readonly ICurrentUserContext currentUserContext;

        public int renterId { get; private set; }

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
            renterId = currentUserContext.currentUserId;
            var allRequests = requestService
                .GetRequestsForRenter(renterId)
                .OrderByDescending(request => request.StartDate)
                .ToImmutableList();
            SetAllItems(allRequests);
        }

        public string? TryCancelRequest(int requestId)
        {
            var rawResult = requestService.CancelRequest(requestId, renterId);
            if (rawResult >= MinimumSuccessfulEntityId)
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