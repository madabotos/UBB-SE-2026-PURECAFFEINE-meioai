using System;
using Property_and_Management.Src.Interface;

namespace Property_and_Management.Src.Model
{
    public class Notification : IEntity
    {
        public int Identifier { get; set; }
        public User User { get; set; }
        public DateTime Timestamp { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public NotificationType Type { get; set; } = NotificationType.Informational;
        public int? RelatedRequestIdentifier { get; set; }

        public Notification()
        {
        }
        public Notification(int identifier, User user, DateTime timestamp, string title, string body,
                            NotificationType type = NotificationType.Informational, int? relatedRequestIdentifier = null)
        {
            Identifier = identifier;
            User = user;
            Timestamp = timestamp;
            Title = title;
            Body = body;
            Type = type;
            RelatedRequestIdentifier = relatedRequestIdentifier;
        }
    }
}


