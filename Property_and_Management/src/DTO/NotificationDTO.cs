using System;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.DTO
{
    public class NotificationDTO : IDTO<Notification>
    {
        public int Id { get; set; }
        public UserDTO User { get; set; }
        public DateTime Timestamp { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }

        // UI computed properties are fine here — they only use DTO data
        public string TimeDisplay => Timestamp.ToString("hh:mm tt");

        public NotificationDTO() { }
    }
}
