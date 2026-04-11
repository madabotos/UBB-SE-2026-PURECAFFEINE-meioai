using System;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.DataTransferObjects
{
    public class NotificationDataTransferObject : IDataTransferObject<Notification>
    {
        public int Id { get; set; }
        public UserDataTransferObject User { get; set; }
        public DateTime Timestamp { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }

        public NotificationType Type { get; set; } = NotificationType.Informational;
        public int? RelatedRequestId { get; set; }

        // UI computed properties are fine here — they only use Data Transfer Object data
        public string TimeDisplay => Timestamp.ToString("hh:mm tt");
        public bool IsActionable => Type == NotificationType.OfferReceived;

        public NotificationDataTransferObject() { }
    }
}
