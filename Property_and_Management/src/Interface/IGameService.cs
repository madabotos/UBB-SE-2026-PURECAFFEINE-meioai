using System.Collections.Immutable;
using Property_and_Management.Src.DataTransferObjects;

namespace Property_and_Management.Src.Interface
{
    public interface IGameService
    {
        /// <summary>Add a new game.</summary>
        /// <param name="game">The added game data.</param>
        void AddGame(GameDataTransferObject game);

        /// <summary>Update an existing game identified by its identifier.</summary>
        /// <param name="gameIdentifier">The identifier of the game.</param>
        /// <param name="game">The updated game data.</param>
        public void UpdateGameByIdentifier(int gameIdentifier, GameDataTransferObject game);

        /// <summary>Delete a game by its identifier and return the deleted item.</summary>
        /// <param name="gameIdentifier">The identifier of the game.</param>
        /// <returns>The deleted <see cref="GameDataTransferObject"/>.</returns>
        public GameDataTransferObject DeleteGameByIdentifier(int gameIdentifier);

        /// <summary>Get a game by its identifier and returns it.</summary>
        /// <param name="gameIdentifier">The identifier of the game.</param>
        /// <returns>The <see cref="GameDataTransferObject"/>.</returns>
        public GameDataTransferObject GetGameByIdentifier(int gameIdentifier);

        /// <summary>Return all games of a given owner.</summary>
        /// <param name="ownerIdentifier">The identifier of the owner.</param>
        /// <returns>A list of <see cref="GameDataTransferObject"/> objects for the specified owner.</returns>
        public ImmutableList<GameDataTransferObject> GetGamesForOwner(int ownerIdentifier);

        /// <summary>Return all games in the system.</summary>
        ImmutableList<GameDataTransferObject> GetAllGames();
    }
}

