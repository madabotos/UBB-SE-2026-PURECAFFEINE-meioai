using Property_and_Management.src.Interface;

namespace Property_and_Management.src
{
    public sealed class CurrentUserContext : ICurrentUserContext
    {
        public int CurrentUserIdentifier { get; }

        public CurrentUserContext(int currentUserIdentifier)
        {
            CurrentUserIdentifier = currentUserIdentifier;
        }
    }
}
