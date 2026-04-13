using System;
using System.Collections.Generic;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;

namespace Property_and_Management.Src.Viewmodels
{
    public class EditGameViewModel
    {
        private const int MissingOwnerIdentifier = 0;
        private const int NoValidationErrors = 0;
        private const decimal InvalidOrEmptyPriceValue = 0m;

        private readonly IGameService gameService;

        /// <summary>Identifier of the loaded game. Read-only (per UI-EDG-03).</summary>
        public int GameIdentifier { get; private set; }

        /// <summary>Identifier of the game's owner. Read-only (per UI-EDG-03).</summary>
        public int OwnerIdentifier { get; private set; }

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

        public EditGameViewModel(IGameService gameService)
        {
            this.gameService = gameService;
        }

        public void LoadGame(int incomingGameIdentifier)
        {
            // Parameter is intentionally named 'incomingGameIdentifier' so it
            // does not shadow the GameIdentifier property. The old parameter
            // name silently caused every save to target game_id = 0.
            var existingGame = gameService.GetGameByIdentifier(incomingGameIdentifier);
            if (existingGame == null)
            {
                return;
            }

            GameIdentifier = existingGame.Identifier;
            OwnerIdentifier = existingGame.Owner?.Identifier ?? MissingOwnerIdentifier;

            Name = existingGame.Name;
            Price = existingGame.Price;
            MinimumPlayers = existingGame.MinimumPlayerNumber;
            MaximumPlayers = existingGame.MaximumPlayerNumber;
            Description = existingGame.Description;
            IsActive = existingGame.IsActive;
            Image = existingGame.Image;
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

        public ViewOperationResult SubmitGameUpdate()
        {
            var validationErrors = ValidateInputs();
            if (validationErrors.Count > NoValidationErrors)
            {
                return ViewOperationResult.Failure(
                    Constants.DialogTitles.ValidationError,
                    string.Join(Environment.NewLine, validationErrors));
            }

            UpdateGame();
            return ViewOperationResult.Success();
        }

        /// <summary>
        /// Parse a raw price string from the view's NumberBox and update the
        /// bound <see cref="Price"/> / <see cref="PriceDouble"/>. Falls back
        /// to zero when the input cannot be parsed so downstream validation
        /// reports it as an invalid price rather than a stale value.
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

        public GameDataTransferObject UpdateGame()
        {
            if (ValidateInputs().Count > NoValidationErrors)
            {
                return null;
            }

            Image = GameInputHelper.EnsureImageOrDefault(Image, AppDomain.CurrentDomain.BaseDirectory);

            var updatedGameDataTransferObject = new GameDataTransferObject
            {
                Identifier = GameIdentifier,
                Owner = new UserDataTransferObject { Identifier = OwnerIdentifier },
                Name = Name,
                Price = Price,
                MinimumPlayerNumber = MinimumPlayers,
                MaximumPlayerNumber = MaximumPlayers,
                Description = Description,
                Image = Image,
                IsActive = IsActive
            };

            gameService.UpdateGameByIdentifier(GameIdentifier, updatedGameDataTransferObject);
            return updatedGameDataTransferObject;
        }
    }
}
