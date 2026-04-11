using System.Collections.Immutable;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Interface
{
    public interface INotificationRepository : IRepository<Notification>
    {
        /// <summary>
        /// Returns notifications addressed to the specified user.
        /// </summary>
        /// <param name="userIdentifier">User id.</param>
        /// <returns>Immutable list of notifications for the user.</returns>
        ImmutableList<Notification> GetNotificationsByUser(int userIdentifier);

        /// <summary>
        /// Returns actionable notifications linked to a specific request.
        /// Used for deduplication and cleanup after offer approve/deny.
        /// </summary>
        ImmutableList<Notification> GetActionableByRequestId(int requestIdentifier);

        /// <summary>
        /// Deletes all notifications linked to a specific request.
        /// </summary>
        void DeleteByRequestId(int requestIdentifier);
    }
}

