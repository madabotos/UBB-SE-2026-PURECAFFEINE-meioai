using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Viewmodels
{
    public class CreateGameViewModel
    {
        private readonly IGameService _gameService;

        // UI Binding Properties
        public string Name { get; set; } = string.Empty;
        public double Price { get; set; }
        public int MinPlayers { get; set; } = 1;
        public int MaxPlayers { get; set; } = 4;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public byte[] Image { get; set; } = null;

        public int CurrentUserId { get; set; } = (App.Current as App).CurrentUserID;

        public CreateGameViewModel(IGameService gameService)
        {
            _gameService = gameService;
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

        public GameDTO SaveGame()
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

            var newGameDto = new GameDTO
            {
                Id = 0,
                Owner = new UserDTO { Id = CurrentUserId },
                Name = Name,
                Price = Price,
                MinimumPlayerNumber = MinPlayers,
                MaximumPlayerNumber = MaxPlayers,
                Description = Description,
                Image = Image,
                IsActive = IsActive
            };

            _gameService.AddGame(newGameDto);
            return newGameDto;
        }
    }
}
