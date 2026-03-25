using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq; // You need this for Skip() and Take()
using System.Runtime.CompilerServices;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;

namespace Property_and_Management.src.Viewmodels
{
    public class ListingsViewModel : INotifyPropertyChanged
    {
        private readonly IGameService _gameService;
        private readonly int _currentUserId;

        // 1. We keep a hidden list of ALL games in memory
        private ObservableCollection<GameDTO> _allListings = new ObservableCollection<GameDTO>();

        // 2. The UI binds to THIS list, which only holds the current page's games
        public ObservableCollection<GameDTO> PagedListings { get; set; } = new ObservableCollection<GameDTO>();

        // Pagination Properties
        private int _pageSize = 5; // Change this to show more/less items per page
        private int _currentPage = 1;

        public int CurrentPage
        {
            get => _currentPage;
            set { _currentPage = value; OnPropertyChanged(); UpdatePagedListings(); }
        }

        public int PageCount => (int)Math.Ceiling((double)_allListings.Count / _pageSize) == 0 ? 1 : (int)Math.Ceiling((double)_allListings.Count / _pageSize);

        public string ShowingText => $"Showing {PagedListings.Count} of {_allListings.Count} listings";

        public ListingsViewModel(IGameService gameService, int currentUserId)
        {
            _gameService = gameService;
            _currentUserId = currentUserId;
            LoadGames();
        }

        public void LoadGames()
        {
            _allListings.Clear();
            var games = _gameService.GetGamesForOwner(_currentUserId);

            foreach (var game in games)
            {
                _allListings.Add(game);
            }

            CurrentPage = 1; // This automatically calls UpdatePagedListings()
        }

        private void UpdatePagedListings()
        {
            PagedListings.Clear();

            // The Magic: Skip previous pages, Take the next batch!
            var pagedData = _allListings.Skip((CurrentPage - 1) * _pageSize).Take(_pageSize);

            foreach (var item in pagedData)
            {
                PagedListings.Add(item);
            }

            // Tell the UI to update the text counters
            OnPropertyChanged(nameof(PageCount));
            OnPropertyChanged(nameof(ShowingText));
        }

        public void NextPage()
        {
            if (CurrentPage < PageCount) CurrentPage++;
        }

        public void PrevPage()
        {
            if (CurrentPage > 1) CurrentPage--;
        }

        public void DeleteGame(GameDTO game)
        {
            _gameService.DeleteGameById(game.Id);
            _allListings.Remove(game);

            // If deleting the last item on a page leaves it empty, step back a page
            if (CurrentPage > PageCount) CurrentPage = PageCount;
            else UpdatePagedListings(); // Otherwise just refresh the current page
        }

        // INotifyPropertyChanged implementation for updating text in real-time
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
