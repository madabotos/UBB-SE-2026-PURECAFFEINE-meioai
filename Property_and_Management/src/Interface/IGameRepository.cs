using Property_and_Management.src.Model;
using System.Collections.Immutable;

namespace Property_and_Management.src.Interface
{
    public interface IGameRepository : IRepository<Game>
    {
        /// <summary>
        /// Gets games owned by the specified user.
        /// </summary>
        /// <param name="ownerId">Owner user id.</param>
        /// <returns>Immutable list of games.</returns>
        ImmutableList<Game> GetGamesByOwner(int ownerId);
    }
}
