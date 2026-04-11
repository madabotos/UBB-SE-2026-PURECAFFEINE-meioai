using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Property_and_Management;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;

namespace Property_and_Management.src.Viewmodels
{
    public class EditGameViewModel
    {
        private const int DefaultMinimumPlayers = 1;
        private const int DefaultMaximumPlayers = 4;
        private const int MinimumNameLength = 5;
        private const int MaximumNameLength = 30;
        private const decimal MinimumAllowedPrice = 1m;
        private const int MinimumPlayerCount = 1;
        private const int MinimumDescriptionLength = 10;
        private const int MaximumDescriptionLength = 500;
        private const int MissingOwnerId = 0;
        private const int NoValidationErrors = 0;
        private const int EmptyImageLength = 0;

        private readonly IGameService _gameService;

        // Read-only identifiers (per requirement UI-EDG-03)
        public int GameId { get; private set; }
        public int OwnerId { get; private set; }

        // UI Binding Properties
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public double PriceDouble
        {
            get => (double)Price;
            set => Price = (decimal)value;
        }
        public int MinPlayers { get; set; } = DefaultMinimumPlayers;
        public int MaxPlayers { get; set; } = DefaultMaximumPlayers;
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
                OwnerId = existingGame.Owner?.Id ?? MissingOwnerId;

                Name = existingGame.Name;
                Price = existingGame.Price;
                MinPlayers = existingGame.MinimumPlayerNumber;
                MaxPlayers = existingGame.MaximumPlayerNumber;
                Description = existingGame.Description;
                IsActive = existingGame.IsActive;
                Image = existingGame.Image;
            }
        }

        public List<string> ValidateInputs()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name) || Name.Length < MinimumNameLength || Name.Length > MaximumNameLength)
                errors.Add(Constants.ValidationMessages.NameLengthRange(MinimumNameLength, MaximumNameLength));
            if (Price < MinimumAllowedPrice)
                errors.Add(Constants.ValidationMessages.PriceMinimum(MinimumAllowedPrice));
            if (MinPlayers < MinimumPlayerCount)
                errors.Add(Constants.ValidationMessages.MinimumPlayerCount(MinimumPlayerCount));
            if (MaxPlayers < MinPlayers)
                errors.Add(Constants.ValidationMessages.MaximumPlayerCountComparedToMinimum);
            if (string.IsNullOrWhiteSpace(Description) || Description.Length < MinimumDescriptionLength || Description.Length > MaximumDescriptionLength)
                errors.Add(Constants.ValidationMessages.DescriptionLengthRange(MinimumDescriptionLength, MaximumDescriptionLength));

            return errors;
        }

        public GameDTO UpdateGame()
        {
            if (ValidateInputs().Count > NoValidationErrors) return null;

            if (Image == null || Image.Length == EmptyImageLength)
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
                    Image = Array.Empty<byte>();
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
