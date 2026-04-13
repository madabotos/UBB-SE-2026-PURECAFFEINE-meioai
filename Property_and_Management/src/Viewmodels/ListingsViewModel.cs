using System.Collections.Immutable;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;

namespace Property_and_Management.Src.Viewmodels
{
    public class ListingsViewModel : PagedViewModel<GameDataTransferObject>
    {
        private const int NoActiveRentalsCount = 0;
        private const string DeleteSuccessMessageTemplate =
            "There are {0} active rentals for this game. It was removed successfully.";

        private readonly IGameService gameService;
        private readonly int currentUserIdentifier;

        public ListingsViewModel(IGameService gameService, int currentUserIdentifier)
        {
            this.gameService = gameService;
            this.currentUserIdentifier = currentUserIdentifier;
            Reload();
        }

        /// <summary>
        /// Convenience alias kept because views invoke LoadGames() after external
        /// mutations (e.g. navigation back from the create/edit pages).
        /// </summary>
        public void LoadGames() => Reload();

        protected override void Reload()
        {
            var games = gameService.GetGamesForOwner(currentUserIdentifier);
            SetAllItems(games.ToImmutableList());
        }

        public override string ShowingText => $"Showing {DisplayedCount} of {TotalCount} games";

        public void DeleteGame(GameDataTransferObject game)
        {
            gameService.DeleteGameByIdentifier(game.Identifier);
            Reload();
        }

        public ViewOperationResult TryDeleteGame(GameDataTransferObject game)
        {
            try
            {
                DeleteGame(game);
                return ViewOperationResult.Success(
                    Constants.DialogTitles.GameRemoved,
                    string.Format(DeleteSuccessMessageTemplate, NoActiveRentalsCount));
            }
            catch (System.InvalidOperationException invalidOperationException)
            {
                return ViewOperationResult.Failure(
                    Constants.DialogTitles.CannotDeleteGame,
                    invalidOperationException.Message);
            }
        }
    }
}
