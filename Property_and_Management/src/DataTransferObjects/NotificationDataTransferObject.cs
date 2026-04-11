using System;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.DataTransferObjects
{
    public class NotificationDataTransferObject : IDataTransferObject<Notification>
    {
        public int Identifier { get; set; }
        public UserDataTransferObject User { get; set; }
        public DateTime Timestamp { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }

        public NotificationType Type { get; set; } = NotificationType.Informational;
        public int? RelatedRequestIdentifier { get; set; }

        // UI computed properties are fine here — they only use Data Transfer Object data
        public string TimeDisplay => Timestamp.ToString("hh:mm tt");
        public bool IsActionable => Type == NotificationType.OfferReceived;

        public NotificationDataTransferObject()
        {
        }
    }
}

