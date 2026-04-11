using System.Collections.Immutable;
using Property_and_Management.src.DataTransferObjects;

namespace Property_and_Management.src.Interface
{
    public interface IUserService
    {
        ImmutableList<UserDataTransferObject> GetUsersExcept(int excludeUserId);
    }
}
