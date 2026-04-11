using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Property_and_Management.src.DataTransferObjects;
using Property_and_Management.src.Interface;

namespace Property_and_Management.src.Viewmodels
{
    public class ListingsViewModel : INotifyPropertyChanged
    {
        private const int DefaultPageSize = 3;
        private const int FirstPageNumber = 1;
        private const int PageStep = 1;

        private readonly IGameService _gameService;
        private readonly int _currentUserId;

        // We keep a hidden list of all games in memory
        private ObservableCollection<GameDataTransferObject> _allListings = new ObservableCollection<GameDataTransferObject>();

        // The UI binds to THIS list, which only holds the current page's games
        public ObservableCollection<GameDataTransferObject> PagedListings { get; set; } = new ObservableCollection<GameDataTransferObject>();

        // Pagination Properties
        private int _pageSize = DefaultPageSize; // Change this to show more/less items per page
        private int _currentPage = FirstPageNumber;

        public int CurrentPage
        {
            get => _currentPage;
            set { _currentPage = value; OnPropertyChanged(); UpdatePagedListings(); }
        }

        public int PageCount => Math.Max(FirstPageNumber, (int)Math.Ceiling((double)_allListings.Count / _pageSize));

        public string ShowingText => $"Showing {PagedListings.Count} of {_allListings.Count} games";

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

            CurrentPage = FirstPageNumber; // This automatically calls UpdatePagedListings()
        }

        private void UpdatePagedListings()
        {
            PagedListings.Clear();

            // Skip previous pages, Take the next batch!
            var pagedData = _allListings.Skip((CurrentPage - FirstPageNumber) * _pageSize).Take(_pageSize);

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
            if (CurrentPage < PageCount) CurrentPage += PageStep;
        }

        public void PrevPage()
        {
            if (CurrentPage > FirstPageNumber) CurrentPage -= PageStep;
        }

        public void DeleteGame(GameDataTransferObject game)
        {
            _gameService.DeleteGameByIdentifier(game.Id);
            _allListings.Remove(game);

            // If deleting the last item on a page leaves it empty, step back a page
            if (CurrentPage > PageCount) CurrentPage = PageCount;
            else UpdatePagedListings(); // Otherwise just refresh the current page
        }

        // INotifyPropertyChanged implementation for updating text in real-time
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
