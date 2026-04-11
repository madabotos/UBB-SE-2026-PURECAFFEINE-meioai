using System;
using System.Collections.Immutable;
using Property_and_Management.src.DataTransferObjects;

namespace Property_and_Management.src.Interface
{
    public interface INotificationService : IObservable<NotificationDataTransferObject>
    {
        /// <summary>Get a notification by its identifier.</summary>
        /// <param name="notificationIdentifier">The identifier of the notification.</param>
        /// <returns>The <see cref="NotificationDataTransferObject"/> with the specified identifier.</returns>
        NotificationDataTransferObject GetNotificationByIdentifier(int notificationIdentifier);

        /// <summary>Delete a notification by its identifier and return the deleted item.</summary>
        /// <param name="notificationIdentifier">The identifier of the notification.</param>
        /// <returns>The deleted <see cref="NotificationDataTransferObject"/>.</returns>
        NotificationDataTransferObject DeleteNotificationByIdentifier(int notificationIdentifier);

        /// <summary>Update an existing notification identified by identifier.</summary>
        /// <param name="notificationIdentifier">The identifier of the notification to update.</param>
        /// <param name="notification">The updated notification data.</param>
        void UpdateNotificationByIdentifier(int notificationIdentifier, NotificationDataTransferObject notification);

        /// <summary>Send a notification to a specific user.</summary>
        /// <param name="userIdentifier">The identifier of the user.</param>
        /// <param name="notification">The notification to send.</param>
        void SendNotificationToUser(int userIdentifier, NotificationDataTransferObject notification);

        /// <summary>Return all notifications for a given user.</summary>
        /// <param name="userIdentifier">The identifier of the user.</param>
        /// <returns>A list of <see cref="NotificationDataTransferObject"/> objects for the specified user.</returns>
        ImmutableList<NotificationDataTransferObject> GetNotificationsForUser(int userIdentifier);

        /// <summary>
        /// Subscribes to recive notifications for the given userIdentifier
        /// </summary>
        /// <param name="userIdentifier"></param>
        void SubscribeToServer(int userIdentifier);

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
        void ScheduleUpcomingRentalReminder(int renterIdentifier, int ownerIdentifier, string gameName, DateTime startDate);

        /// <summary>
        /// Deletes all notifications linked to a specific request (cleanup after offer approve/deny).
        /// </summary>
        void DeleteNotificationsByRequestId(int requestIdentifier);
    }
}

