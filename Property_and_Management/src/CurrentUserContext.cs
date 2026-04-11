using Property_and_Management.src.Interface;

namespace Property_and_Management.src
{
    public sealed class CurrentUserContext : ICurrentUserContext
    {
        public int CurrentUserId { get; }

        public CurrentUserContext(int currentUserId)
        {
            CurrentUserId = currentUserId;
        }
    }
}
