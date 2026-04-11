using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Mapper
{
    public class UserMapper : IMapper<User, UserDTO>
    {
        public UserDTO ToDTO(User user)
        {
            if (user == null) return null;

            return new UserDTO
            {
                Id = user.Id,
                DisplayName = user.DisplayName
            };
        }

        public User ToModel(UserDTO userDto)
        {
            if (userDto == null) return null;

            return new User
            {
                Id = userDto.Id,
                DisplayName = userDto.DisplayName
            };
        }
    }
}
