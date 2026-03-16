using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Authentication.OnlineId;

namespace Property_and_Management.src.Model
{
    class Notification(int id, int userId, DateTime timestamp, string title, string body) : IEntity
    {
        public int Id { get; set; } = id;
        public int UserId { get; set; } = userId;
        public DateTime Timestamp { get; set; } = timestamp;
        public string Title { get; set; } = title;
        public string Body { get; set; } = body;
    }
}
