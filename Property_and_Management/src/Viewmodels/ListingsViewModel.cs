using System;
using System.Collections.ObjectModel;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Service;

namespace Property_and_Management.src.Viewmodels
{
    public class ListingsViewModel
    {
        private readonly GameService _gameService;
        private readonly int _currentUserId;

        // The UI binds to this collection
        public ObservableCollection<GameDTO> Listings { get; set; } = new ObservableCollection<GameDTO>();

        public ListingsViewModel(GameService gameService, int currentUserId)
        {
            _gameService = gameService;
            _currentUserId = currentUserId;
            LoadGames();
        }

        public void LoadGames()
        {
            Listings.Clear();
            // UI-LST-01: Fetch games where owner_id equals current user
            var games = _gameService.GetGamesForOwner(_currentUserId);
            
            foreach (var game in games)
            {
                Listings.Add(game);
            }
        }

        public void DeleteGame(GameDTO game)
        {
            // UI-LST-03: Deletes the Game entity
            _gameService.DeleteGameById(game.Id);
            Listings.Remove(game);
        }
    }
}
