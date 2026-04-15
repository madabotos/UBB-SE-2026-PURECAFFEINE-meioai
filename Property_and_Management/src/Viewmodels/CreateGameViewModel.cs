using System;
using System.Collections.Generic;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;

namespace Property_and_Management.Src.Viewmodels
{
    public class CreateGameViewModel
    {
        private const int NoValidationErrors = 0;
        private const int NewEntityId = 0;
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

        public int currentUserId => currentUserContext.currentUserId;

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

        public ViewOperationResult SubmitCreateGame()
        {
            var validationErrors = ValidateInputs();
            if (validationErrors.Count > NoValidationErrors)
            {
                return ViewOperationResult.Failure(
                    Constants.DialogTitles.ValidationError,
                    string.Join(Environment.NewLine, validationErrors));
            }

            SaveGame();
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

        public GameDTO SaveGame()
        {
            if (ValidateInputs().Count > NoValidationErrors)
            {
                return null;
            }

            Image = GameInputHelper.EnsureImageOrDefault(Image, AppDomain.CurrentDomain.BaseDirectory);

            var newGameDTO = new GameDTO
            {
                Id = NewEntityId,
                Owner = new UserDTO { Id = currentUserId },
                Name = Name,
                Price = Price,
                MinimumPlayerNumber = MinimumPlayers,
                MaximumPlayerNumber = MaximumPlayers,
                Description = Description,
                Image = Image,
                IsActive = IsActive
            };

            gameService.AddGame(newGameDTO);
            return newGameDTO;
        }
    }
}