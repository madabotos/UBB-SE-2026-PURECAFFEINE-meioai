using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Viewmodels
{
    public class CreateRentalViewModel : INotifyPropertyChanged
    {
        private readonly IGameService _gameService;
        private readonly IRentalService _rentalService;
        private readonly IMapper<User, UserDTO> _userMapper;
        private readonly IUserRepository _userRepository;

        public int CurrentUserId => (App.Current as App)?.CurrentUserID ?? 1;

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
                                     IUserRepository userRepository, IMapper<User, UserDTO> userMapper)
        {
            _gameService = gameService;
            _rentalService = rentalService;
            _userRepository = userRepository;
            _userMapper = userMapper;
            LoadData();
        }

        public void LoadData()
        {
            MyGames.Clear();
            foreach (var game in _gameService.GetGamesForOwner(CurrentUserId).Where(g => g.IsActive))
                MyGames.Add(game);

            AvailableRenters.Clear();
            foreach (var user in _userRepository.GetAll().Where(u => u.Id != CurrentUserId))
                AvailableRenters.Add(_userMapper.ToDTO(user));
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
                var rental = new Rental(
                    id: 0,
                    game: new Game { Id = SelectedGame.Id },
                    renter: new User { Id = SelectedRenter.Id },
                    owner: new User { Id = CurrentUserId },
                    startDate: StartDate.Value.DateTime,
                    endDate: EndDate.Value.DateTime);

                _rentalService.CreateConfirmedRental(rental);
                return null; // success
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
