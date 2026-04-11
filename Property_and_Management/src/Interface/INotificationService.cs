using System;
using System.Collections.Immutable;
using Property_and_Management.src.DataTransferObjects;

namespace Property_and_Management.src.Interface
{
    public interface INotificationService : IObservable<NotificationDataTransferObject>
    {
        /// <summary>Get a notification by its identifier.</summary>
        /// <param name="id">The identifier of the notification.</param>
        /// <returns>The <see cref="NotificationDataTransferObject"/> with the specified identifier.</returns>
        NotificationDataTransferObject GetNotificationById(int id);

        /// <summary>Delete a notification by its identifier and return the deleted item.</summary>
        /// <param name="id">The identifier of the notification.</param>
        /// <returns>The deleted <see cref="NotificationDataTransferObject"/>.</returns>
        NotificationDataTransferObject DeleteNotificationById(int id);

        /// <summary>Update an existing notification identified by id.</summary>
        /// <param name="id">The identifier of the notification to update.</param>
        /// <param name="notification">The updated notification data.</param>
        void UpdateNotificationById(int id, NotificationDataTransferObject notification);

        /// <summary>Send a notification to a specific user.</summary>
        /// <param name="userId">The identifier of the user.</param>
        /// <param name="notification">The notification to send.</param>
        void SendNotificationToUser(int userId, NotificationDataTransferObject notification);

        /// <summary>Return all notifications for a given user.</summary>
        /// <param name="userId">The identifier of the user.</param>
        /// <returns>A list of <see cref="NotificationDataTransferObject"/> objects for the specified user.</returns>
        ImmutableList<NotificationDataTransferObject> GetNotificationsForUser(int userId);

        /// <summary>
        /// Subscribes to recive notifications for the given userId
        /// </summary>
        /// <param name="userId"></param>
        void SubscribeToServer(int userId);

        /// <summary>
        /// Starts the listening on the client
        /// </summary>
        void StartListening();

        /// <summary>
        /// Stops the listening on the client
        /// </summary>
        void StopListening();

        /// <summary>
        /// Schedule an upcoming rental reminder 24 hours before the rental start for both renter and owner.
        /// </summary>
        void ScheduleUpcomingRentalReminder(int renterId, int ownerId, string gameName, DateTime startDate);

        /// <summary>
        /// Deletes all notifications linked to a specific request (cleanup after offer approve/deny).
        /// </summary>
        void DeleteNotificationsByRequestId(int requestId);
    }
}
