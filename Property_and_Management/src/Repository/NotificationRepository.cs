using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;
using Windows.ApplicationModel.Activation;

namespace Property_and_Management.src.Repository
{
    public class NotificationRepository : DatabaseRepository<Notification>
    {
        public ImmutableList<Notification> GetNotificationsByUser(int userId)
        {
            return SelectWhere($"user_id = {userId}");
        }
    }
}
