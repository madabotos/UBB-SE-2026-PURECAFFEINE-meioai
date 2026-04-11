using System;
using System.Collections.Generic;
using Property_and_Management;
using Property_and_Management.src.DataTransferObjects;
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
        private const int MissingOwnerIdentifier = 0;
        private const int NoValidationErrors = 0;
        private const int EmptyImageLength = 0;

        private readonly IGameService _gameService;

        // Read-only identifiers (per requirement UI-EDG-03)
        public int gameIdentifier { get; private set; }
        public int ownerIdentifier { get; private set; }

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

        public void LoadGame(int gameIdentifier)
        {
            var existingGame = _gameService.GetGameByIdentifier(gameIdentifier);
            if (existingGame != null)
            {
                gameIdentifier = existingGame.Identifier;
                ownerIdentifier = existingGame.Owner?.Identifier ?? MissingOwnerIdentifier;

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
            return GameInputHelper.BuildValidationErrors(
                Name,
                Price,
                MinPlayers,
                MaxPlayers,
                Description,
                MinimumNameLength,
                MaximumNameLength,
                MinimumAllowedPrice,
                MinimumPlayerCount,
                MinimumDescriptionLength,
                MaximumDescriptionLength);
        }

        public GameDataTransferObject UpdateGame()
        {
            if (ValidateInputs().Count > NoValidationErrors) return null;

            Image = GameInputHelper.EnsureImageOrDefault(Image, AppDomain.CurrentDomain.BaseDirectory);

            // ✅ Object initializer — no constructors, no entity references
            var updatedGameDataTransferObject = new GameDataTransferObject
            {
                Identifier = gameIdentifier,
                Owner = new UserDataTransferObject { Identifier = ownerIdentifier },
                Name = Name,
                Price = Price,
                MinimumPlayerNumber = MinPlayers,
                MaximumPlayerNumber = MaxPlayers,
                Description = Description,
                Image = Image,
                IsActive = IsActive
            };

            _gameService.UpdateGameByIdentifier(gameIdentifier, updatedGameDataTransferObject);
            return updatedGameDataTransferObject;
        }
    }
}


