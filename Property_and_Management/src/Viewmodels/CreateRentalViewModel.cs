using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Property_and_Management.src.DataTransferObjects;
using Property_and_Management.src.Interface;

namespace Property_and_Management.src.Viewmodels
{
    public class CreateRentalViewModel : INotifyPropertyChanged
    {
        private readonly IGameService _gameService;
        private readonly IRentalService _rentalService;
        private readonly IUserService _userService;
        private readonly ICurrentUserContext _currentUserContext;

        public int CurrentUserIdentifier => _currentUserContext.CurrentUserIdentifier;

        public ObservableCollection<GameDataTransferObject> MyGames { get; set; } = new();
        public ObservableCollection<UserDataTransferObject> AvailableRenters { get; set; } = new();

        private GameDataTransferObject _selectedGame;
        public GameDataTransferObject SelectedGame
        {
            get => _selectedGame;
            set { _selectedGame = value; OnPropertyChanged(); }
        }

        private UserDataTransferObject _selectedRenter;
        public UserDataTransferObject SelectedRenter
        {
            get => _selectedRenter;
            set { _selectedRenter = value; OnPropertyChanged(); }
        }

        private DateTimeOffset? _startDate;
        public DateTimeOffset? StartDate
        {
            get => _startDate;
            set { _startDate = value; OnPropertyChanged(); }
        }

        private DateTimeOffset? _endDate;
        public DateTimeOffset? EndDate
        {
            get => _endDate;
            set { _endDate = value; OnPropertyChanged(); }
        }

        public CreateRentalViewModel(IGameService gameService, IRentalService rentalService,
                                     IUserService userService, ICurrentUserContext currentUserContext)
        {
            _gameService = gameService;
            _rentalService = rentalService;
            _userService = userService;
            _currentUserContext = currentUserContext;
            LoadData();
        }

        public void LoadData()
        {
            MyGames.Clear();
            foreach (var game in _gameService.GetGamesForOwner(CurrentUserIdentifier))
            {
                if (game.IsActive)
                    MyGames.Add(game);
            }

            AvailableRenters.Clear();
            foreach (var user in _userService.GetUsersExcept(CurrentUserIdentifier))
                AvailableRenters.Add(user);
        }

        public bool ValidateInputs()
        {
            if (SelectedGame == null) return false;
            if (SelectedRenter == null) return false;
            return DateRangeValidationHelper.HasValidFutureDateRange(StartDate, EndDate);
        }

        public string SaveRental()
        {
            if (!ValidateInputs()) return "Validation failed.";

            try
            {
                _rentalService.CreateConfirmedRental(
                    SelectedGame.Identifier,
                    SelectedRenter.Identifier,
                    CurrentUserIdentifier,
                    StartDate.Value.DateTime,
                    EndDate.Value.DateTime);
                return null;
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}


