using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;

namespace Property_and_Management.src.Viewmodels
{
    public class EditGameViewModel
    {
        private readonly IGameService _gameService;

        // Read-only identifiers (per requirement UI-EDG-03)
        public int GameId { get; private set; }
        public int OwnerId { get; private set; }

        // UI Binding Properties
        public string Name { get; set; } = string.Empty;
        public double Price { get; set; }
        public int MinPlayers { get; set; } = 1;
        public int MaxPlayers { get; set; } = 4;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public byte[] Image { get; set; } = null;

        public EditGameViewModel(IGameService gameService)
        {
            _gameService = gameService;
        }

        public void LoadGame(int gameId)
        {
            var existingGame = _gameService.GetGameById(gameId);
            if (existingGame != null)
            {
                GameId = existingGame.Id;
                OwnerId = existingGame.Owner?.Id ?? 0;

                Name = existingGame.Name;
                Price = existingGame.Price;
                MinPlayers = existingGame.MinimumPlayerNumber;
                MaxPlayers = existingGame.MaximumPlayerNumber;
                Description = existingGame.Description;
                IsActive = existingGame.IsActive;
                Image = existingGame.Image;
            }
        }

        public bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(Name) || Name.Length < 5 || Name.Length > 30)
                return false;
            if (Price <= 0)
                return false;
            if (MinPlayers < 1)
                return false;
            if (MaxPlayers < MinPlayers)
                return false;
            if (string.IsNullOrWhiteSpace(Description) || Description.Length < 10 || Description.Length > 500)
                return false;
            return true;
        }

        public GameDTO UpdateGame()
        {
            if (!ValidateInputs()) return null;

            if (Image == null || Image.Length == 0)
            {
                try
                {
                    string defaultImagePath = System.IO.Path.Combine(
                        System.AppDomain.CurrentDomain.BaseDirectory,
                        "Assets",
                        "default-game-placeholder.jpg");
                    Image = System.IO.File.ReadAllBytes(defaultImagePath);
                }
                catch
                {
                    Image = new byte[0];
                }
            }

            // ✅ Object initializer — no constructors, no entity references
            var updatedGameDto = new GameDTO
            {
                Id = GameId,
                Owner = new UserDTO { Id = OwnerId },
                Name = Name,
                Price = Price,
                MinimumPlayerNumber = MinPlayers,
                MaximumPlayerNumber = MaxPlayers,
                Description = Description,
                Image = Image,
                IsActive = IsActive
            };

            _gameService.UpdateGameById(GameId, updatedGameDto);
            return updatedGameDto;
        }
    }
}
