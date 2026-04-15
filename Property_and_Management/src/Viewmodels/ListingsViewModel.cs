using System.Collections.Immutable;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;

namespace Property_and_Management.Src.Viewmodels
{
    public class ListingsViewModel : PagedViewModel<GameDTO>
    {
        private const int NoActiveRentalsCount = 0;
        private const string DeleteSuccessMessageTemplate =
            "There are {0} active rentals for this game. It was removed successfully.";

        private readonly IGameService gameService;
        private readonly int currentUserId;

        public ListingsViewModel(IGameService gameService, int currentUserId)
        {
            this.gameService = gameService;
            this.currentUserId = currentUserId;
            Reload();
        }

        public void LoadGames() => Reload();

        protected override void Reload()
        {
            var games = gameService.GetGamesForOwner(currentUserId);
            SetAllItems(games.ToImmutableList());
        }

        public override string ShowingText => $"Showing {DisplayedCount} of {TotalCount} games";

        public void DeleteGame(GameDTO game)
        {
            gameService.DeleteGameByIdentifier(game.Id);
            Reload();
        }

        public ViewOperationResult TryDeleteGame(GameDTO game)
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
            catch (System.Exception exception)
            {
                return ViewOperationResult.Failure(
                    Constants.DialogTitles.CannotDeleteGame,
                    string.IsNullOrWhiteSpace(exception.Message)
                        ? Constants.DialogMessages.UnexpectedErrorOccurred
                        : exception.Message);
            }
        }
    }
}