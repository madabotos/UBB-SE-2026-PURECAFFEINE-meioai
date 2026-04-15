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
        private readonly IMapper<User, UserDTO> userMapper;

        public UserService(IUserRepository userRepository, IMapper<User, UserDTO> userMapper)
        {
            this.userRepository = userRepository;
            this.userMapper = userMapper;
        }

        public ImmutableList<UserDTO> GetUsersExcept(int excludeUserId) =>
            userRepository.GetAll()
                .Where(user => user.Id != excludeUserId)
                .Select(user => userMapper.ToDTO(user))
                .ToImmutableList();
    }
}