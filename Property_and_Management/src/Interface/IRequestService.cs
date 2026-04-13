using System;
using System.Collections.Immutable;
using Property_and_Management.Src.DataTransferObjects;

namespace Property_and_Management.Src.Interface
{
    /// <summary>
    /// Application-layer API for creating, approving, denying, offering and
    /// cancelling rental requests. Fallible operations return
    /// <see cref="Result{TSuccess, TError}"/> with a domain-specific error enum
    /// so callers don't have to check sentinel-int return codes.
    /// </summary>
    public interface IRequestService
    {
        /// <summary>
        /// Returns all requests created by the specified renter.
        /// </summary>
        ImmutableList<RequestDataTransferObject> GetRequestsForRenter(int renterIdentifier);

        /// <summary>
        /// Returns all requests addressed to the specified owner.
        /// </summary>
        ImmutableList<RequestDataTransferObject> GetRequestsForOwner(int ownerIdentifier);

        /// <summary>
        /// Creates a new pending request. On success returns the new request id.
        /// </summary>
        Result<int, CreateRequestError> CreateRequest(
            int gameIdentifier,
            int renterIdentifier,
            int ownerIdentifier,
            DateTime startDate,
            DateTime endDate);

        /// <summary>
        /// Owner approves a pending request directly, creating the rental atomically.
        /// Returns the new rental id on success.
        /// </summary>
        Result<int, ApproveRequestError> ApproveRequest(int requestIdentifier, int ownerIdentifier);

        /// <summary>
        /// Owner declines a pending request with a reason. Returns the request id on success.
        /// </summary>
        Result<int, DenyRequestError> DenyRequest(int requestIdentifier, int ownerIdentifier, string reason);

        /// <summary>
        /// Renter cancels their own pending request. Returns the cancelled request
        /// identifier on success, or a negative <see cref="CancelRequestError"/>
        /// code on failure (legacy sentinel-int contract preserved).
        /// </summary>
        int CancelRequest(int requestIdentifier, int cancellingUserIdentifier);

        /// <summary>
        /// Cleanup hook: when a game is deactivated/deleted, declines every pending
        /// request referencing it and notifies the renters.
        /// </summary>
        void OnGameDeactivated(int gameIdentifier);

        /// <summary>
        /// Checks whether the given date window is free for the given game, enforcing
        /// the 48 hour symmetric buffer around existing rentals and requests.
        /// </summary>
        bool CheckAvailability(int gameIdentifier, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Returns the list of booked date ranges for a game within a given month/year,
        /// suitable for rendering a calendar. Defaults to the current month.
        /// </summary>
        ImmutableList<(DateTime StartDate, DateTime EndDate)> GetBookedDates(
            int gameIdentifier,
            int month,
            int year);

        /// <summary>
        /// Owner offers their game against a pending request. In the current flow
        /// this immediately approves the request and creates the rental atomically.
        /// Returns the new rental id on success.
        /// </summary>
        Result<int, OfferError> OfferGame(int requestIdentifier, int offeringUserIdentifier);
    }
}
