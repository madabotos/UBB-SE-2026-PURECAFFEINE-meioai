using System;
using Property_and_Management.src.Interface;

namespace Property_and_Management.src.Viewmodels
{
    public class ChatViewModel
    {
        private readonly IRequestService _requestService;

        // The ID of the request we are currently looking at
        public int RequestId { get; set; }

        // MOCK USER: We hardcode a user ID for now to simulate being logged in.
        // Assuming user ID 1 is the owner of this game.
        public int CurrentUserId { get; set; } = (App.Current as App)?.CurrentUserID ?? 1;

        public ChatViewModel(IRequestService requestService)
        {
            _requestService = requestService ?? throw new ArgumentNullException(nameof(requestService));
        }

        // Called when the user clicks Approve
        public int Approve()
        {
            return _requestService.ApproveRequest(RequestId, CurrentUserId);
        }

        // Called when the user clicks Deny
        public int Deny()
        {
            return _requestService.DenyRequest(RequestId, CurrentUserId, "Declined by owner via Chat UI.");
        }
    }
}
