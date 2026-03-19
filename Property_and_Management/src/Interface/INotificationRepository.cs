using Property_and_Management.src.Model;
using System.Collections.Immutable;

namespace Property_and_Management.src.Interface
{
    public interface INotificationRepository : IRepository<Notification>
    {
        /// <summary>
        /// Returns notifications addressed to the specified user.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <returns>Immutable list of notifications for the user.</returns>
        ImmutableList<Notification> GetNotificationsByUser(int userId);
    }
}
