using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Property_and_Management.src.DTO;

namespace Property_and_Management.src.Interface
{
    public interface IGameService
    {
        /// <summary>Add a new game.</summary>
        /// <param name="game">The added game data.</param>
        void AddGame(GameDTO game);

        /// <summary>Update an existing game identified by id.</summary>
        /// <param name="id">The identifier of the game.</param>
        /// <param name="game">The updated game data.</param>
        public void UpdateGameById(int id, GameDTO game);

        /// <summary>Delete a game by its identifier and return the deleted item.</summary>
        /// <param name="id">The identifier of the game.</param>
        /// <returns>The deleted <see cref="GameDTO"/>.</returns>
        public GameDTO DeleteGameById(int id);

        /// <summary>Get a game by its identifier and returns it.</summary>
        /// <param name="id">The identifier of the game.</param>
        /// <returns>The <see cref="GameDTO"/>.</returns>
        public GameDTO GetGameById(int id);

        /// <summary>Return all games of a given owner.</summary>
        /// <param name="ownerId">The identifier of the owner.</param>
        /// <returns>A list of <see cref="GameDTO"/> objects for the specified owner.</returns>
        public ImmutableList<GameDTO> GetGamesForOwner(int ownerId);

        /// <summary>Return all games in the system.</summary>
        ImmutableList<GameDTO> GetAllGames();
    }
}
