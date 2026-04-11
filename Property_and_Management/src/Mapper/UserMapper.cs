using Property_and_Management.src.DataTransferObjects;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Mapper
{
    public class UserMapper : IMapper<User, UserDataTransferObject>
    {
        public UserDataTransferObject ToDataTransferObject(User user)
        {
            if (user == null) return null;

            return new UserDataTransferObject
            {
                Id = user.Id,
                DisplayName = user.DisplayName
            };
        }

        public User ToModel(UserDataTransferObject userDataTransferObject)
        {
            if (userDataTransferObject == null) return null;

            return new User
            {
                Id = userDataTransferObject.Id,
                DisplayName = userDataTransferObject.DisplayName
            };
        }
    }
}
