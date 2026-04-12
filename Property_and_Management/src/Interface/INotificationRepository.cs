using System.Collections.Immutable;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Interface
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
        /// Deletes all notifications linked to a specific request.
        /// </summary>
        void DeleteByRequestId(int requestIdentifier);
    }
}

