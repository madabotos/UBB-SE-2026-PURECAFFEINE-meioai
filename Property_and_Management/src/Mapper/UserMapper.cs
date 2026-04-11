using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Mapper
{
    public class UserMapper : IMapper<User, UserDataTransferObject>
    {
        public UserDataTransferObject ToDataTransferObject(User user)
        {
            if (user == null)
            {
                return null;
            }

            return new UserDataTransferObject
            {
                Identifier = user.Identifier,
                DisplayName = user.DisplayName
            };
        }

        public User ToModel(UserDataTransferObject userDataTransferObject)
        {
            if (userDataTransferObject == null)
            {
                return null;
            }

            return new User
            {
                Identifier = userDataTransferObject.Identifier,
                DisplayName = userDataTransferObject.DisplayName
            };
        }
    }
}

