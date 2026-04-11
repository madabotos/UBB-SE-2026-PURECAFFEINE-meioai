using System;
using System.Collections.Generic;
using System.Linq;
using Property_and_Management;
using Property_and_Management.src.DTO;
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
        private const int NewEntityId = 0;

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

        public int CurrentUserId => _currentUserContext.CurrentUserId;

        public CreateGameViewModel(IGameService gameService, ICurrentUserContext currentUserContext)
        {
            _gameService = gameService;
            _currentUserContext = currentUserContext;
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

        public GameDTO SaveGame()
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

            var newGameDto = new GameDTO
            {
                Id = NewEntityId,
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
