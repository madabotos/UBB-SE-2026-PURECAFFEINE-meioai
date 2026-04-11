using System.Collections.Immutable;
using System.Linq;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Service
{
    public class UserService : IUserService
    {
        private readonly IUserRepository userRepository;
        private readonly IMapper<User, UserDataTransferObject> userMapper;

        public UserService(IUserRepository userRepository, IMapper<User, UserDataTransferObject> userMapper)
        {
            this.userRepository = userRepository;
            this.userMapper = userMapper;
        }

        public ImmutableList<UserDataTransferObject> GetUsersExcept(int excludeUserIdentifier) =>
            userRepository.GetAll()
                .Where(user => user.Identifier != excludeUserIdentifier)
                .Select(user => userMapper.ToDataTransferObject(user))
                .ToImmutableList();
    }
}


