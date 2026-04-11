using System;
using System.Collections.Generic;
using Property_and_Management;
using Property_and_Management.src.DataTransferObjects;
using Property_and_Management.src.Interface;

namespace Property_and_Management.src.Viewmodels
{
    public class CreateGameViewModel
    {
        private const int DefaultMinimumPlayers = 1;
        private const int DefaultMaximumPlayers = 4;
        private const int MinimumNameLength = 5;
        private const int MaximumNameLength = 30;
        private const decimal MinimumAllowedPrice = 1m;
        private const int MinimumPlayerCount = 1;
        private const int MinimumDescriptionLength = 10;
        private const int MaximumDescriptionLength = 500;
        private const int NoValidationErrors = 0;
        private const int EmptyImageLength = 0;
        private const int NewEntityIdentifier = 0;

        private readonly IGameService _gameService;
        private readonly ICurrentUserContext _currentUserContext;

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

        public int CurrentUserIdentifier => _currentUserContext.CurrentUserIdentifier;

        public CreateGameViewModel(IGameService gameService, ICurrentUserContext currentUserContext)
        {
            _gameService = gameService;
            _currentUserContext = currentUserContext;
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

        public GameDataTransferObject SaveGame()
        {
            if (ValidateInputs().Count > NoValidationErrors) return null;

            Image = GameInputHelper.EnsureImageOrDefault(Image, AppDomain.CurrentDomain.BaseDirectory);

            var newgameDataTransferObject = new GameDataTransferObject
            {
                Identifier = NewEntityIdentifier,
                Owner = new UserDataTransferObject { Identifier = CurrentUserIdentifier },
                Name = Name,
                Price = Price,
                MinimumPlayerNumber = MinPlayers,
                MaximumPlayerNumber = MaxPlayers,
                Description = Description,
                Image = Image,
                IsActive = IsActive
            };

            _gameService.AddGame(newgameDataTransferObject);
            return newgameDataTransferObject;
        }
    }
}



