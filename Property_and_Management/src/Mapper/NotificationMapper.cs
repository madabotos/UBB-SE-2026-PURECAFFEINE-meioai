using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public NotificationDTO ToDTO(Notification entity)
        {
            if (entity == null) return null;

            return new NotificationDTO
            {
                Id = entity.Id,
                User = _userMapper.ToDTO(entity.User),
                Timestamp = entity.Timestamp,
                Title = entity.Title,
                Body = entity.Body,
                Type = entity.Type,
                RelatedRequestId = entity.RelatedRequestId
            };
        }

        public Notification ToModel(NotificationDTO dto)
        {
            if (dto == null) return null;

            return new Notification
            {
                Id = dto.Id,
                User = _userMapper.ToModel(dto.User),
                Timestamp = dto.Timestamp,
                Title = dto.Title,
                Body = dto.Body,
                Type = dto.Type,
                RelatedRequestId = dto.RelatedRequestId
            };
        }
    }
}
