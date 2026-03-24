using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Property_and_Management.src.DTO;

namespace Property_and_Management.src.Interface
{
    public interface IRequestService
    {
        ImmutableList<RequestDTO> GetRequestsForRenter(int renterId);
        ImmutableList<RequestDTO> GetRequestsForOwner(int ownerId);

        /// <returns>New request ID, or a negative CreateRequestError code.</returns>
        int CreateRequest(int gameId, int renterId, int ownerId, DateTime startDate, DateTime endDate);

        /// <returns>New rental ID, or a negative ApproveRequestError code.</returns>
        int ApproveRequest(int requestId, int ownerId);

        /// <returns>Deleted request ID, or a negative DenyRequestError code.</returns>
        int DenyRequest(int requestId, int ownerId, string reason);

        void CancelRequest(int requestId);
        void OnGameDeactivated(int gameId);

        bool CheckAvailability(int gameId, DateTime startDate, DateTime endDate);
        ImmutableList<(DateTime, DateTime)> GetBookedDates(int gameId, int month, int year);   
    }
}
