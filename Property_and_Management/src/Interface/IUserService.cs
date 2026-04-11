using System.Collections.Immutable;
using Property_and_Management.src.DTO;

namespace Property_and_Management.src.Interface
{
    public interface IUserService
    {
        ImmutableList<UserDTO> GetUsersExcept(int excludeUserId);
    }
}
