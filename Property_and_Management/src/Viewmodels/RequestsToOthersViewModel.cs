using System.Collections.Immutable;
using System.Linq;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;

namespace Property_and_Management.Src.Viewmodels
{
    public class RequestsToOthersViewModel : PagedViewModel<RequestDTO>
    {
        private readonly IRequestService rentalRequestService;
        private readonly ICurrentUserContext currentUserContext;

        public int CurrentRenterUserId { get; private set; }

        public RequestsToOthersViewModel(IRequestService rentalRequestService, ICurrentUserContext currentUserContext)
        {
            this.rentalRequestService = rentalRequestService;
            this.currentUserContext = currentUserContext;
            Reload();
        }

        public override string ShowingText => $"Showing {DisplayedCount} of {TotalCount} requests";

        public void LoadRequests() => Reload();

        protected override void Reload()
        {
            CurrentRenterUserId = currentUserContext.CurrentUserId;
            var renterRequestsSortedByNewest = rentalRequestService
                .GetRequestsForRenter(CurrentRenterUserId)
                .OrderByDescending(request => request.StartDate)
                .ToImmutableList();
            SetAllItems(renterRequestsSortedByNewest);
        }

        public string? TryCancelRequest(int requestIdToCancel)
        {
            var cancellationResult = rentalRequestService.CancelRequest(requestIdToCancel, CurrentRenterUserId);
            if (cancellationResult.IsSuccess)
            {
                Reload();
                return null;
            }

            return cancellationResult.Error switch
            {
                CancelRequestError.NotFound => "Request not found.",
                CancelRequestError.Unauthorized => "You are not authorized to cancel this request.",
                _ => Constants.DialogMessages.UnexpectedErrorOccurred
            };
        }
    }
}
