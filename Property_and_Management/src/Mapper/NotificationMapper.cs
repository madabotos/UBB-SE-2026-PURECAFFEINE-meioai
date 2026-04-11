using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Mapper
{
    public class NotificationMapper : IMapper<Notification, NotificationDTO>
    {
        private readonly IMapper<User, UserDTO> _userMapper;

        public NotificationMapper(IMapper<User, UserDTO> userMapper)
        {
            _userMapper = userMapper;
        }

        public NotificationDTO ToDTO(Notification notification)
        {
            if (notification == null) return null;

            return new NotificationDTO
            {
                Id = notification.Id,
                User = _userMapper.ToDTO(notification.User),
                Timestamp = notification.Timestamp,
                Title = notification.Title,
                Body = notification.Body,
                Type = notification.Type,
                RelatedRequestId = notification.RelatedRequestId
            };
        }

        public Notification ToModel(NotificationDTO notificationDto)
        {
            if (notificationDto == null) return null;

            return new Notification
            {
                Id = notificationDto.Id,
                User = _userMapper.ToModel(notificationDto.User),
                Timestamp = notificationDto.Timestamp,
                Title = notificationDto.Title,
                Body = notificationDto.Body,
                Type = notificationDto.Type,
                RelatedRequestId = notificationDto.RelatedRequestId
            };
        }
    }
}
