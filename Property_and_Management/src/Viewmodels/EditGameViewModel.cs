using System;
using System.Collections.Generic;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;

namespace Property_and_Management.Src.Viewmodels
{
    public class EditGameViewModel
    {
        private const int MissingOwnerId = 0;
        private const int NoValidationErrors = 0;
        private const decimal InvalidOrEmptyPriceValue = 0m;

        private readonly IGameService gameService;

        public int gameId { get; private set; }

        public int ownerId { get; private set; }

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

        public void LoadGame(int incomingGameId)
        {
            var existingGame = gameService.GetGameByIdentifier(incomingGameId);
            if (existingGame == null)
            {
                return;
            }

            gameId = existingGame.Id;
            ownerId = existingGame.Owner?.Id ?? MissingOwnerId;

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

        public void SetPriceFromText(string priceText)
        {
            if (PriceInputParser.TryParsePriceInput(priceText, out var parsedPrice))
            {
                PriceDouble = parsedPrice;
                return;
            }

            Price = InvalidOrEmptyPriceValue;
        }

        public GameDTO UpdateGame()
        {
            if (ValidateInputs().Count > NoValidationErrors)
            {
                return null;
            }

            Image = GameInputHelper.EnsureImageOrDefault(Image, AppDomain.CurrentDomain.BaseDirectory);

            var updatedGameDTO = new GameDTO
            {
                Id = gameId,
                Owner = new UserDTO { Id = ownerId },
                Name = Name,
                Price = Price,
                MinimumPlayerNumber = MinimumPlayers,
                MaximumPlayerNumber = MaximumPlayers,
                Description = Description,
                Image = Image,
                IsActive = IsActive
            };

            gameService.UpdateGameByIdentifier(gameId, updatedGameDTO);
            return updatedGameDTO;
        }
    }
}