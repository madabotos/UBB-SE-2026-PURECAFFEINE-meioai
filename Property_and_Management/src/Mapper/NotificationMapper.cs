using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Mapper
{
    public class NotificationMapper : IMapper<Notification, NotificationDataTransferObject>
    {
        private readonly IMapper<User, UserDataTransferObject> userMapper;

        public NotificationMapper(IMapper<User, UserDataTransferObject> userMapper)
        {
            this.userMapper = userMapper;
        }

        public NotificationDataTransferObject ToDataTransferObject(Notification notification)
        {
            if (notification == null)
            {
                return null;
            }

            return new NotificationDataTransferObject
            {
                Identifier = notification.Identifier,
                User = userMapper.ToDataTransferObject(notification.User),
                Timestamp = notification.Timestamp,
                Title = notification.Title,
                Body = notification.Body,
                Type = notification.Type,
                RelatedRequestIdentifier = notification.RelatedRequestIdentifier
            };
        }

        public Notification ToModel(NotificationDataTransferObject notificationDataTransferObject)
        {
            if (notificationDataTransferObject == null)
            {
                return null;
            }

            return new Notification
            {
                Identifier = notificationDataTransferObject.Identifier,
                User = userMapper.ToModel(notificationDataTransferObject.User),
                Timestamp = notificationDataTransferObject.Timestamp,
                Title = notificationDataTransferObject.Title,
                Body = notificationDataTransferObject.Body,
                Type = notificationDataTransferObject.Type,
                RelatedRequestIdentifier = notificationDataTransferObject.RelatedRequestIdentifier
            };
        }
    }
}


