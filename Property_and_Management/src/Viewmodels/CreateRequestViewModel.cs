using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;

namespace Property_and_Management.Src.Viewmodels
{
    public class CreateRequestViewModel : INotifyPropertyChanged
    {
        private readonly IGameService gameService;
        private readonly IRequestService requestService;
        private readonly ICurrentUserContext currentUserContext;

        public int CurrentUserIdentifier => currentUserContext.CurrentUserIdentifier;

        public ObservableCollection<GameDataTransferObject> AvailableGames { get; set; } = new();

        private GameDataTransferObject selectedGame;
        public GameDataTransferObject SelectedGame
        {
            get => selectedGame;
            set
            {
                selectedGame = value;
                OnPropertyChanged();
            }
        }

        private System.DateTimeOffset? startDate;
        public System.DateTimeOffset? StartDate
        {
            get => startDate;
            set
            {
                startDate = value;
                OnPropertyChanged();
            }
        }

        private System.DateTimeOffset? endDate;
        public System.DateTimeOffset? EndDate
        {
            get => endDate;
            set
            {
                endDate = value;
                OnPropertyChanged();
            }
        }

        public CreateRequestViewModel(IGameService gameService, IRequestService requestService,
                                      ICurrentUserContext currentUserContext)
        {
            this.gameService = gameService;
            this.requestService = requestService;
            this.currentUserContext = currentUserContext;
            LoadGames();
        }

        public void LoadGames()
        {
            AvailableGames.Clear();
            var games = gameService.GetAllGames()
                .Where(game => game.IsActive && game.Owner?.Identifier != CurrentUserIdentifier);
            foreach (var game in games)
            {
                AvailableGames.Add(game);
            }
        }

        public bool ValidateInputs()
        {
            if (SelectedGame == null)
            {
                return false;
            }

            return DateRangeValidationHelper.HasValidFutureDateRange(StartDate, EndDate);
        }

        /// <summary>
        /// Submit the request using the currently selected game and dates.
        /// Returns a user-friendly error message, or <c>null</c> on success.
        /// Keeps the service-namespace error enums out of the view layer.
        /// </summary>
        public string? TrySubmitRequest()
        {
            if (!ValidateInputs())
            {
                return Constants.DialogMessages.CreateRequestValidationError;
            }

            var result = requestService.CreateRequest(
                SelectedGame.Identifier,
                CurrentUserIdentifier,
                SelectedGame.Owner.Identifier,
                StartDate.Value.DateTime,
                EndDate.Value.DateTime);

            if (result.IsSuccess)
            {
                return null;
            }

            return result.Error switch
            {
                CreateRequestError.OwnerCannotRent => "You cannot rent your own game.",
                CreateRequestError.DatesUnavailable => "The selected dates are not available.",
                CreateRequestError.GameDoesNotExist => "The selected game no longer exists.",
                _ => Constants.DialogMessages.UnexpectedErrorOccurred
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
