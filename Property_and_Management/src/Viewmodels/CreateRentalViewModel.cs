using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;

namespace Property_and_Management.src.Viewmodels
{
    public class CreateRentalViewModel : INotifyPropertyChanged
    {
        private readonly IGameService _gameService;
        private readonly IRentalService _rentalService;
        private readonly IUserService _userService;
        private readonly ICurrentUserContext _currentUserContext;

        public int CurrentUserId => _currentUserContext.CurrentUserId;

        public ObservableCollection<GameDTO> MyGames { get; set; } = new();
        public ObservableCollection<UserDTO> AvailableRenters { get; set; } = new();

        private GameDTO _selectedGame;
        public GameDTO SelectedGame
        {
            get => _selectedGame;
            set { _selectedGame = value; OnPropertyChanged(); }
        }

        private UserDTO _selectedRenter;
        public UserDTO SelectedRenter
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
            foreach (var game in _gameService.GetGamesForOwner(CurrentUserId))
            {
                if (game.IsActive)
                    MyGames.Add(game);
            }

            AvailableRenters.Clear();
            foreach (var user in _userService.GetUsersExcept(CurrentUserId))
                AvailableRenters.Add(user);
        }

        public bool ValidateInputs()
        {
            if (SelectedGame == null) return false;
            if (SelectedRenter == null) return false;
            if (StartDate == null || EndDate == null) return false;
            if (StartDate.Value.Date >= EndDate.Value.Date) return false;
            if (StartDate.Value.Date < DateTimeOffset.Now.Date) return false;
            return true;
        }

        public string SaveRental()
        {
            if (!ValidateInputs()) return "Validation failed.";

            try
            {
                _rentalService.CreateConfirmedRental(
                    SelectedGame.Id,
                    SelectedRenter.Id,
                    CurrentUserId,
                    StartDate.Value.DateTime,
                    EndDate.Value.DateTime);
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
