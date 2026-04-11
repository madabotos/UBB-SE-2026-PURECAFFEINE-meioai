using System.Collections.Immutable;
using System.Linq;
using Property_and_Management.src.DataTransferObjects;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Service
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper<User, UserDataTransferObject> _userMapper;

        public UserService(IUserRepository userRepository, IMapper<User, UserDataTransferObject> userMapper)
        {
            _userRepository = userRepository;
            _userMapper = userMapper;
        }

        public ImmutableList<UserDataTransferObject> GetUsersExcept(int excludeUserId) =>
            _userRepository.GetAll()
                .Where(user => user.Id != excludeUserId)
                .Select(user => _userMapper.ToDataTransferObject(user))
                .ToImmutableList();
    }
}
