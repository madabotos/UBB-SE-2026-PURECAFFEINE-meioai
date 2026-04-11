using System.Collections.Immutable;
using System;
using Property_and_Management.src.DataTransferObjects;

namespace Property_and_Management.src.Interface
{
    public interface IRequestService
    {
        /// <summary>
        /// Returns ImmutableList<RequestDataTransferObject> of all requests made by a specific renter.
        /// </summary>
        /// <param name="renterIdentifier"></param>
        /// <returns></returns>
        ImmutableList<RequestDataTransferObject> GetRequestsForRenter(int renterIdentifier);

        /// <summary>
        /// Returns owner's incoming rental requests as immutable list.
        /// </summary>
        /// <param name="ownerIdentifier"></param>
        /// <returns></returns>
        ImmutableList<RequestDataTransferObject> GetRequestsForOwner(int ownerIdentifier);

        /// <summary>
        /// Creates new request with game ID, renter/owner IDs, and date range; returns new request ID.
        /// </summary>
        /// <param name="gameIdentifier"></param>
        /// <param name="renterIdentifier"></param>
        /// <param name="ownerIdentifier"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        int CreateRequest(int gameIdentifier, int renterIdentifier, int ownerIdentifier, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Owner approves specific request; returns status code.
        /// </summary>
        /// <param name="requestIdentifier"></param>
        /// <param name="ownerIdentifier"></param>
        /// <returns></returns>
        int ApproveRequest(int requestIdentifier, int ownerIdentifier);

        /// <summary>
        /// Owner rejects with reason.
        /// </summary>
        /// <param name="requestIdentifier"></param>
        /// <param name="ownerIdentifier"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        int DenyRequest(int requestIdentifier, int ownerIdentifier, string reason);

        /// <summary>
        /// Any party cancels existing request (void).
        /// </summary>
        /// <param name="requestIdentifier"></param>
        void CancelRequest(int requestIdentifier);

        /// <summary>
        /// Handles cleanup when game/property is deactivated (void).
        /// </summary>
        /// <param name="gameIdentifier"></param>
        void OnGameDeactivated(int gameIdentifier);

        /// <summary>
        /// Returns bool if dates are free for game.
        /// </summary>
        /// <param name="gameIdentifier"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        bool CheckAvailability(int gameIdentifier, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Returns ImmutableList<(DateTime, DateTime)> of booked date ranges for calendar/month view.
        /// </summary>
        ImmutableList<(DateTime, DateTime)> GetBookedDates(int gameIdentifier, int month, int year);

        /// <summary>
        /// Owner offers their game to fulfill a request. Sets status to OfferPending and notifies the requester.
        /// </summary>
        int OfferGame(int requestIdentifier, int offeringUserIdentifier);

        /// <summary>
        /// Requester approves a pending offer. Creates a rental, cleans up the request, and notifies both parties.
        /// </summary>
        int ApproveOffer(int requestIdentifier, int renterIdentifier);

        /// <summary>
        /// Requester denies a pending offer. Resets request to Open and notifies the owner.
        /// </summary>
        int DenyOffer(int requestIdentifier, int renterIdentifier);
    }
}

