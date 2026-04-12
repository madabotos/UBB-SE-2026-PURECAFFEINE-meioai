using System.Collections.Immutable;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Interface
{
    public interface IGameRepository : IRepository<Game>
    {
        /// <summary>
        /// Gets games owned by the specified user.
        /// </summary>
        /// <param name="ownerIdentifier">Owner user id.</param>
        /// <returns>Immutable list of games.</returns>
        ImmutableList<Game> GetGamesByOwner(int ownerIdentifier);
    }
}

