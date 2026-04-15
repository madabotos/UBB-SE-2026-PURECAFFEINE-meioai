using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;

namespace Property_and_Management.Src.Viewmodels
{
    public class CreateRentalViewModel : INotifyPropertyChanged
    {
        private const string ValidationFailedMessage = "Validation failed.";

        private readonly IGameService gameService;
        private readonly IRentalService rentalService;
        private readonly IUserService userService;
        private readonly ICurrentUserContext currentUserContext;

        public int currentUserId => currentUserContext.currentUserId;

        public ObservableCollection<GameDTO> MyGames { get; set; } = new();
        public ObservableCollection<UserDTO> AvailableRenters { get; set; } = new();

        private GameDTO selectedGame;
        public GameDTO SelectedGame
        {
            get => selectedGame;
            set
            {
                selectedGame = value;
                OnPropertyChanged();
            }
        }

        private UserDTO selectedRenter;
        public UserDTO SelectedRenter
        {
            get => selectedRenter;
            set
            {
                selectedRenter = value;
                OnPropertyChanged();
            }
        }

        private DateTimeOffset? startDate;
        public DateTimeOffset? StartDate
        {
            get => startDate;
            set
            {
                startDate = value;
                OnPropertyChanged();
            }
        }

        private DateTimeOffset? endDate;
        public DateTimeOffset? EndDate
        {
            get => endDate;
            set
            {
                endDate = value;
                OnPropertyChanged();
            }
        }

        public CreateRentalViewModel(IGameService gameService, IRentalService rentalService,
                                     IUserService userService, ICurrentUserContext currentUserContext)
        {
            this.gameService = gameService;
            this.rentalService = rentalService;
            this.userService = userService;
            this.currentUserContext = currentUserContext;
            LoadData();
        }

        public void LoadData()
        {
            MyGames.Clear();
            foreach (var game in gameService.GetGamesForOwner(currentUserId))
            {
                if (game.IsActive)
                {
                    MyGames.Add(game);
                }
            }

            AvailableRenters.Clear();
            foreach (var user in userService.GetUsersExcept(currentUserId))
            {
                AvailableRenters.Add(user);
            }
        }

        public bool ValidateInputs()
        {
            if (SelectedGame == null)
            {
                return false;
            }

            if (SelectedRenter == null)
            {
                return false;
            }

            return DateRangeValidationHelper.HasValidFutureDateRange(StartDate, EndDate);
        }

        public ViewOperationResult CreateRental()
        {
            if (!ValidateInputs())
            {
                return ViewOperationResult.Failure(
                    Constants.DialogTitles.ValidationError,
                    Constants.DialogMessages.CreateRentalValidationError);
            }

            try
            {
                rentalService.CreateConfirmedRental(
                    SelectedGame.Id,
                    SelectedRenter.Id,
                    currentUserId,
                    StartDate.Value.DateTime,
                    EndDate.Value.DateTime);
                return ViewOperationResult.Success();
            }
            catch (Exception exception)
            {
                return ViewOperationResult.Failure(Constants.DialogTitles.RentalFailed, exception.Message);
            }
        }

        public string? SaveRental()
        {
            var createResult = CreateRental();
            if (createResult.IsSuccess)
            {
                return null;
            }

            if (createResult.DialogTitle == Constants.DialogTitles.ValidationError)
            {
                return ValidationFailedMessage;
            }

            return createResult.DialogMessage;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}