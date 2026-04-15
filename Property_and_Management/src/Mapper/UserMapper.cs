using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Mapper
{
    public class UserMapper : IMapper<User, UserDTO>
    {
        public UserDTO ToDTO(User user)
        {
            if (user == null)
            {
                return null;
            }

            return new UserDTO
            {
                Id = user.Id,
                DisplayName = user.DisplayName
            };
        }

        public User ToModel(UserDTO UserDTO)
        {
            if (UserDTO == null)
            {
                return null;
            }

            return new User
            {
                Id = UserDTO.Id,
                DisplayName = UserDTO.DisplayName
            };
        }
    }
}