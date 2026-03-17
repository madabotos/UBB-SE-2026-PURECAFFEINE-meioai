using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Property_and_Management.src.Attributes;
using Property_and_Management.src.Interface;
using Windows.Security.Authentication.OnlineId;

namespace Property_and_Management.src.Model
{
    [SqlTableDefinition("Notifications")]
    public class Notification : IEntity
    {
        [SqlTableFieldDefinition("notification_id", IsPrimaryKey = true)]
        public int Id { get; set; }

        [SqlTableFieldDefinition("user_id")]
        public int UserId { get; set; }

        [SqlTableFieldDefinition("timestamp")]
        public DateTime Timestamp { get; set; }

        [SqlTableFieldDefinition("title")]
        public string Title { get; set; }

        [SqlTableFieldDefinition("body")]
        public string Body { get; set; }

        public Notification(int id, int userId, DateTime timestamp, string title, string body)
        {
            Id = id;
            UserId = userId;
            Timestamp = timestamp;
            Title = title;
            Body = body;
        }

        public static IEntity BuildFromParameters(Dictionary<string, object> parameters)
        {
            return new Notification(
                id: (int)parameters["notification_id"],
                userId: (int)parameters["user_id"],
                timestamp: (DateTime)parameters["timestamp"],
                title: (string)parameters["title"],
                body: (string)parameters["body"]
            );
        }
    }
}
