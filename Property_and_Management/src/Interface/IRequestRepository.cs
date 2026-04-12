using System;
using System.Collections.Immutable;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Interface
{
    /// <summary>
    /// Persistence-layer contract for the Requests table. Methods are intentionally
    /// narrow and stateless; all business decisions (what counts as "overlap",
    /// which notifications to send, etc.) belong in <see cref="IRequestService"/>.
    /// </summary>
    public interface IRequestRepository : IRepository<Request>
    {
        /// <summary>
        /// Updates only the status and offering user on a request.
        /// </summary>
        void UpdateStatus(int requestIdentifier, RequestStatus status, int? offeringUserIdentifier);

        /// <summary>
        /// Gets requests for which the specified user is the game owner.
        /// </summary>
        ImmutableList<Request> GetRequestsByOwner(int ownerIdentifier);

        /// <summary>
        /// Gets requests created by the specified renter.
        /// </summary>
        ImmutableList<Request> GetRequestsByRenter(int renterIdentifier);

        /// <summary>
        /// Gets requests for the specified game.
        /// </summary>
        ImmutableList<Request> GetRequestsByGame(int gameIdentifier);

        /// <summary>
        /// Returns every request on <paramref name="gameIdentifier"/> whose date window
        /// overlaps <c>[bufferedStart, bufferedEnd)</c>, excluding the request identified
        /// by <paramref name="excludeRequestIdentifier"/>. Used by the service to decide
        /// which requests get cascade-cancelled when a rental is created.
        /// </summary>
        ImmutableList<Request> GetOverlappingRequests(
            int gameIdentifier,
            int excludeRequestIdentifier,
            DateTime bufferedStart,
            DateTime bufferedEnd);

        /// <summary>
        /// Commits the rental-creation cascade inside a single serializable transaction:
        /// delete notifications for the approved request and all pre-computed overlapping
        /// requests, insert the rental, then delete the affected requests. Returns the
        /// new rental identifier. The caller is responsible for deciding which requests
        /// are actually overlapping — this method just applies their fate atomically.
        /// </summary>
        int ApproveAtomically(
            Request approvedRequest,
            ImmutableList<Request> overlappingRequests);
    }
}
