using System;
using System.Collections.Generic;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;

namespace Property_and_Management.Src.Viewmodels
{
    public class CreateGameViewModel
    {
        private const int NoValidationErrors = 0;
        private const int NewEntityIdentifier = 0;
        private const decimal InvalidOrEmptyPriceValue = 0m;

        private readonly IGameService gameService;
        private readonly ICurrentUserContext currentUserContext;

        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public double PriceDouble
        {
            get => (double)Price;
            set => Price = (decimal)value;
        }
        public int MinimumPlayers { get; set; } = Constants.GameValidation.DefaultMinimumPlayers;
        public int MaximumPlayers { get; set; } = Constants.GameValidation.DefaultMaximumPlayers;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public byte[] Image { get; set; } = null;

        public int CurrentUserIdentifier => currentUserContext.CurrentUserIdentifier;

        public CreateGameViewModel(IGameService gameService, ICurrentUserContext currentUserContext)
        {
            this.gameService = gameService;
            this.currentUserContext = currentUserContext;
        }

        public List<string> ValidateInputs()
        {
            return GameInputHelper.BuildValidationErrors(
                Name,
                Price,
                MinimumPlayers,
                MaximumPlayers,
                Description,
                Constants.GameValidation.MinimumNameLength,
                Constants.GameValidation.MaximumNameLength,
                Constants.GameValidation.MinimumAllowedPrice,
                Constants.GameValidation.MinimumPlayerCount,
                Constants.GameValidation.MinimumDescriptionLength,
                Constants.GameValidation.MaximumDescriptionLength);
        }

        /// <summary>
        /// Parse a raw price string from the view's NumberBox and update the
        /// bound <see cref="Price"/> / <see cref="PriceDouble"/>. Falls back
        /// to zero so validation rejects empty/unparseable input.
        /// </summary>
        public void SetPriceFromText(string priceText)
        {
            if (PriceInputParser.TryParsePriceInput(priceText, out var parsedPrice))
            {
                PriceDouble = parsedPrice;
                return;
            }

            Price = InvalidOrEmptyPriceValue;
        }

        public GameDataTransferObject SaveGame()
        {
            if (ValidateInputs().Count > NoValidationErrors)
            {
                return null;
            }

            Image = GameInputHelper.EnsureImageOrDefault(Image, AppDomain.CurrentDomain.BaseDirectory);

            var newgameDataTransferObject = new GameDataTransferObject
            {
                Identifier = NewEntityIdentifier,
                Owner = new UserDataTransferObject { Identifier = CurrentUserIdentifier },
                Name = Name,
                Price = Price,
                MinimumPlayerNumber = MinimumPlayers,
                MaximumPlayerNumber = MaximumPlayers,
                Description = Description,
                Image = Image,
                IsActive = IsActive
            };

            gameService.AddGame(newgameDataTransferObject);
            return newgameDataTransferObject;
        }
    }
}
