using Property_and_Management.src.DataTransferObjects;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Mapper
{
    public class NotificationMapper : IMapper<Notification, NotificationDataTransferObject>
    {
        private readonly IMapper<User, UserDataTransferObject> _userMapper;

        public NotificationMapper(IMapper<User, UserDataTransferObject> userMapper)
        {
            _userMapper = userMapper;
        }

        public NotificationDataTransferObject ToDataTransferObject(Notification notification)
        {
            if (notification == null) return null;

            return new NotificationDataTransferObject
            {
                Id = notification.Id,
                User = _userMapper.ToDataTransferObject(notification.User),
                Timestamp = notification.Timestamp,
                Title = notification.Title,
                Body = notification.Body,
                Type = notification.Type,
                RelatedRequestId = notification.RelatedRequestId
            };
        }

        public Notification ToModel(NotificationDataTransferObject notificationDataTransferObject)
        {
            if (notificationDataTransferObject == null) return null;

            return new Notification
            {
                Id = notificationDataTransferObject.Id,
                User = _userMapper.ToModel(notificationDataTransferObject.User),
                Timestamp = notificationDataTransferObject.Timestamp,
                Title = notificationDataTransferObject.Title,
                Body = notificationDataTransferObject.Body,
                Type = notificationDataTransferObject.Type,
                RelatedRequestId = notificationDataTransferObject.RelatedRequestId
            };
        }
    }
}
