using Property_and_Management.Src.Interface;

namespace Property_and_Management.Src
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
