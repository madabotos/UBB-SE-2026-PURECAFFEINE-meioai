using System.Collections.Immutable;
using System.Linq;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Service
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper<User, UserDTO> _userMapper;

        public UserService(IUserRepository userRepository, IMapper<User, UserDTO> userMapper)
        {
            _userRepository = userRepository;
            _userMapper = userMapper;
        }

        public ImmutableList<UserDTO> GetUsersExcept(int excludeUserId) =>
            _userRepository.GetAll()
                .Where(u => u.Id != excludeUserId)
                .Select(u => _userMapper.ToDTO(u))
                .ToImmutableList();
    }
}
