using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Property_and_Management.src.Service;

namespace Property_and_Management.src.Viewmodels
{
    public class ChatViewModel
    {
        private readonly RequestService _requestService;

        // The ID of the request we are currently looking at
        public int RequestId { get; set; }

        // MOCK USER: We hardcode a user ID for now to simulate being logged in.
        // Assuming user ID 1 is the owner of this game.
        public int CurrentUserId { get; set; } = 1;

        public ChatViewModel()
        {
            // Initialize the service. 
            _requestService = new RequestService();
        }

        // Called when the user clicks Approve
        public void Approve()
        {
            _requestService.ApproveRequest(RequestId, CurrentUserId);
        }

        // Called when the user clicks Deny
        public void Deny()
        {
            _requestService.DenyRequest(RequestId, CurrentUserId, "Declined by owner via Chat UI.");
        }
    }
}
