using System;
using System.Collections.Immutable;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Interface
{
    public interface IRequestRepository : IRepository<Request>
    {
        void UpdateStatus(int requestId, RequestStatus status, int? offeringUserId);

        ImmutableList<Request> GetRequestsByOwner(int ownerId);

        ImmutableList<Request> GetRequestsByRenter(int renterId);

        ImmutableList<Request> GetRequestsByGame(int gameId);

        ImmutableList<Request> GetOverlappingRequests(
            int gameId,
            int excludeRequestId,
            DateTime bufferedStartDate,
            DateTime bufferedEndDate);

        int ApproveAtomically(
            Request approvedRequest,
            ImmutableList<Request> overlappingRequests);
    }
}