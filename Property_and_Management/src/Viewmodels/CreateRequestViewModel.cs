using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;

namespace Property_and_Management.src.Viewmodels
{
    public class CreateRequestViewModel : INotifyPropertyChanged
    {
        private const int InvalidRequestResult = -1;

        private readonly IGameService _gameService;
        private readonly IRequestService _requestService;
        private readonly ICurrentUserContext _currentUserContext;

        public int CurrentUserId => _currentUserContext.CurrentUserId;

        public ObservableCollection<GameDTO> AvailableGames { get; set; } = new();

        private GameDTO _selectedGame;
        public GameDTO SelectedGame
        {
            get => _selectedGame;
            set { _selectedGame = value; OnPropertyChanged(); }
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

        public CreateRequestViewModel(IGameService gameService, IRequestService requestService,
                                      ICurrentUserContext currentUserContext)
        {
            _gameService = gameService;
            _requestService = requestService;
            _currentUserContext = currentUserContext;
            LoadGames();
        }

        public void LoadGames()
        {
            AvailableGames.Clear();
            var games = _gameService.GetAllGames()
                .Where(game => game.IsActive && game.Owner?.Id != CurrentUserId);
            foreach (var game in games)
                AvailableGames.Add(game);
        }

        public bool ValidateInputs()
        {
            if (SelectedGame == null) return false;
            if (StartDate == null || EndDate == null) return false;
            if (StartDate.Value.Date >= EndDate.Value.Date) return false;
            if (StartDate.Value.Date < DateTimeOffset.Now.Date) return false;
            return true;
        }

        public int SaveRequest()
        {
            if (!ValidateInputs()) return InvalidRequestResult;

            return _requestService.CreateRequest(
                SelectedGame.Id,
                CurrentUserId,
                SelectedGame.Owner.Id,
                StartDate.Value.DateTime,
                EndDate.Value.DateTime);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
