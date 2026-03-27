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
    public class UserMapper : IMapper<User, UserDTO>
    {
        public UserDTO ToDTO(User entity)
        {
            if (entity == null) return null;

            return new UserDTO
            {
                Id = entity.Id,
                DisplayName = entity.DisplayName
            };
        }

        public User ToModel(UserDTO dto)
        {
            if (dto == null) return null;

            return new User
            {
                Id = dto.Id,
                DisplayName = dto.DisplayName
            };
        }
    }
}
