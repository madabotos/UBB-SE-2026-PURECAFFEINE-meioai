using System;
using System.Collections.Immutable;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Service;

namespace Property_and_Management.src.Interface
{
    public interface IRequestService
    {
        /// <summary>
        /// Approves a pending request, creates a new rental, and declines overlapping requests.
        /// </summary>
        /// <param name="requestId">The ID of the request to approve.</param>
        /// <param name="ownerId">The ID of the owner approving the request.</param>
        /// <returns>The ID of the new rental, or an error code from <see cref="ApproveRequestError"/>.</returns>
        int ApproveRequest(int requestId, int ownerId);

        /// <summary>
        /// Cancels a pending request. Can be called by the renter without owner approval.
        /// </summary>
        /// <param name="requestId">The ID of the request to cancel.</param>
        void Cancelrequest(int requestId);

        /// <summary>
        /// Checks if a game is available for rental within a specified date range.
        /// </summary>
        /// <param name="gameId">The ID of the game.</param>
        /// <param name="startDate">The start date of the desired rental period.</param>
        /// <param name="endDate">The end date of the desired rental period.</param>
        /// <returns>True if the game is available; otherwise, false.</returns>
        bool CheckAvailability(int gameId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Creates a new rental request for a game.
        /// </summary>
        /// <param name="gameId">The ID of the game.</param>
        /// <param name="renterId">The ID of the renter making the request.</param>
        /// <param name="ownerId">The ID of the game's owner.</param>
        /// <param name="startDate">The start date of the requested rental.</param>
        /// <param name="endDate">The end date of the requested rental.</param>
        /// <returns>The ID of the created request, or an error code from <see cref="CreateRequestError"/>.</returns>
        int CreateRequest(int gameId, int renterId, int ownerId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Denies a pending request.
        /// </summary>
        /// <param name="requestId">The ID of the request to deny.</param>
        /// <param name="ownerId">The ID of the owner denying the request.</param>
        /// <param name="reason">The reason for the denial.</param>
        /// <returns>The ID of the denied request, or an error code from <see cref="DenyRequestError"/>.</returns>
        int DenyRequest(int requestId, int ownerId, string reason);

        /// <summary>
        /// Gets a list of booked dates (including buffer periods) for a game in a specific month and year.
        /// </summary>
        /// <param name="gameId">The ID of the game.</param>
        /// <param name="month">The month to filter by (default is current month).</param>
        /// <param name="year">The year to filter by (default is current year).</param>
        /// <returns>A list of booked date ranges sorted by start date.</returns>
        ImmutableList<(DateTime, DateTime)> GetBookedDates(int gameId, int month = 0, int year = 0);

        /// <summary>
        /// Retrieves all pending requests for a specific owner.
        /// </summary>
        /// <param name="ownerId">The ID of the owner.</param>
        /// <returns>A list of pending requests for the owner.</returns>
        ImmutableList<RequestDTO> GetRequestsForOwner(int ownerId);

        /// <summary>
        /// Retrieves all pending requests for a specific renter.
        /// </summary>
        /// <param name="renterId">The ID of the renter.</param>
        /// <returns>A list of pending requests made by the renter.</returns>
        ImmutableList<RequestDTO> GetRequestsForRenter(int renterId);

        /// <summary>
        /// Declines all pending requests for a game when it is deactivated.
        /// </summary>
        /// <param name="gameId">The ID of the deactivated game.</param>
        void OnGameDeactivated(int gameId);

        /// <summary>
        /// Sets the notification service dependency.
        /// </summary>
        /// <param name="newNotificationService">The notification service instance.</param>
        void SetNotificationService(INotificationService newNotificationService);

        /// <summary>
        /// Sets the rental repository dependency.
        /// </summary>
        /// <param name="newRentalRepository">The rental repository instance.</param>
        void SetRentalRepository(IRentalRepository newRentalRepository);

        /// <summary>
        /// Sets the request repository dependency.
        /// </summary>
        /// <param name="newRequestRepository">The request repository instance.</param>
        void SetRequestRepository(IRequestRepository newRequestRepository);
    }
}
