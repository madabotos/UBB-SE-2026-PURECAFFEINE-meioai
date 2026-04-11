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
        /// <param name="renterId"></param>
        /// <returns></returns>
        ImmutableList<RequestDataTransferObject> GetRequestsForRenter(int renterId);

        /// <summary>
        /// Returns owner's incoming rental requests as immutable list.
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        ImmutableList<RequestDataTransferObject> GetRequestsForOwner(int ownerId);

        /// <summary>
        /// Creates new request with game ID, renter/owner IDs, and date range; returns new request ID.
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="renterId"></param>
        /// <param name="ownerId"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        int CreateRequest(int gameId, int renterId, int ownerId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Owner approves specific request; returns status code.
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        int ApproveRequest(int requestId, int ownerId);

        /// <summary>
        /// Owner rejects with reason.
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="ownerId"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        int DenyRequest(int requestId, int ownerId, string reason);

        /// <summary>
        /// Any party cancels existing request (void).
        /// </summary>
        /// <param name="requestId"></param>
        void CancelRequest(int requestId);

        /// <summary>
        /// Handles cleanup when game/property is deactivated (void).
        /// </summary>
        /// <param name="gameId"></param>
        void OnGameDeactivated(int gameId);

        /// <summary>
        /// Returns bool if dates are free for game.
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        bool CheckAvailability(int gameId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Returns ImmutableList<(DateTime, DateTime)> of booked date ranges for calendar/month view.
        /// </summary>
        ImmutableList<(DateTime, DateTime)> GetBookedDates(int gameId, int month, int year);

        /// <summary>
        /// Owner offers their game to fulfill a request. Sets status to OfferPending and notifies the requester.
        /// </summary>
        int OfferGame(int requestId, int offeringUserId);

        /// <summary>
        /// Requester approves a pending offer. Creates a rental, cleans up the request, and notifies both parties.
        /// </summary>
        int ApproveOffer(int requestId, int renterId);

        /// <summary>
        /// Requester denies a pending offer. Resets request to Open and notifies the owner.
        /// </summary>
        int DenyOffer(int requestId, int renterId);
    }
}
