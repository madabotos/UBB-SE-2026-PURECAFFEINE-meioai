using System;
using System.Collections.Immutable;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Interface
{
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
        /// Atomically approves a request: inserts a rental, finds and deletes all overlapping
        /// open requests (and their notifications), then deletes the approved request itself.
        /// Returns the new rental id and the list of cancelled overlapping requests so the
        /// caller can send notifications to their renters.
        /// </summary>
        (int rentalIdentifier, ImmutableList<Request> OverlappingRequests) ApproveAtomically(
            Request approvedRequest, DateTime bufferedStart, DateTime bufferedEnd);
    }
}

