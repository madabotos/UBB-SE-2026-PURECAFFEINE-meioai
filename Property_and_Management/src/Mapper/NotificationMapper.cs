using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Mapper
{
    public class NotificationMapper : IMapper<Notification, NotificationDTO>
    {
        private readonly IMapper<User, UserDTO> notificationRecipientUserMapper;

        public NotificationMapper(IMapper<User, UserDTO> notificationRecipientUserMapper)
        {
            this.notificationRecipientUserMapper = notificationRecipientUserMapper;
        }

        public NotificationDTO ToDTO(Notification notification)
        {
            if (notification == null)
            {
                return null;
            }

            return new NotificationDTO
            {
                Id = notification.Id,
                User = notificationRecipientUserMapper.ToDTO(notification.User),
                Timestamp = notification.Timestamp,
                Title = notification.Title,
                Body = notification.Body,
                Type = notification.Type,
                RelatedRequestId = notification.RelatedRequestId
            };
        }

        public Notification ToModel(NotificationDTO NotificationDTO)
        {
            if (NotificationDTO == null)
            {
                return null;
            }

            return new Notification
            {
                Id = NotificationDTO.Id,
                User = notificationRecipientUserMapper.ToModel(NotificationDTO.User),
                Timestamp = NotificationDTO.Timestamp,
                Title = NotificationDTO.Title,
                Body = NotificationDTO.Body,
                Type = NotificationDTO.Type,
                RelatedRequestId = NotificationDTO.RelatedRequestId
            };
        }
    }
}