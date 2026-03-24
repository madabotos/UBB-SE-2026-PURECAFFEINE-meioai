using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;
using Property_and_Management.src.Repository;
using Property_and_Management.src.Service;

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

        // Binds to the ComboBox SelectedIndex (0 = Per Hour, 1 = Per Day, 2 = Per Week)
        public int RateTypeIndex { get; set; } = 0;

        public int MinPlayers { get; set; } = 1;
        public int MaxPlayers { get; set; } = 4;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public byte[] Image { get; set; } = null;

        public EditGameViewModel()
        {
            // Instantiate the repository and inject it into the service
            IGameRepository gameRepository = new GameRepository();
            _gameService = new GameService(gameRepository);
        }

        // Call this method when the page loads to pre-populate the form
        public void LoadGame(int gameId)
        {
            var existingGame = _gameService.GetGameById(gameId);

            if (existingGame != null)
            {
                // Store the read-only IDs
                GameId = existingGame.Id;
                OwnerId = existingGame.OwnerId;

                // Pre-populate the form fields with current values 
                Name = existingGame.Name;
                Price = existingGame.Price;
                MinPlayers = existingGame.MinimumPlayerNumber;
                MaxPlayers = existingGame.MaximumPlayerNumber;
                Description = existingGame.Description;
                IsActive = existingGame.IsActive;
                Image = existingGame.Image;

                // Reverse map the RateType enum back to the ComboBox index (0, 1, or 2)
                RateTypeIndex = (int)existingGame.RateType - 1;
            }
        }

        // [UI-EDG-04] The system shall validate all modified inputs against the same constraints 
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

        // Updates the Game in the database and returns the DTO
        public GameDTO UpdateGame()
        {
            if (!ValidateInputs())
            {
                return null;
            }

            // Map the UI ComboBox index back to the RateType Enum
            int rateTypeValue = RateTypeIndex + 1;

            // Create the updated DTO
            var updatedGameDto = new GameDTO(
                id: GameId,
                ownerId: OwnerId, // Preserve original owner
                name: Name,
                price: Price,
                rateType: (RateType)rateTypeValue,
                minimumPlayerNumber: MinPlayers,
                maximumPlayerNumber: MaxPlayers,
                description: Description,
                image: Image,
                isActive: IsActive
            );

            // [UI-EDG-06] On successful validation, update the corresponding record [cite: 134]
            _gameService.UpdateGameById(GameId, updatedGameDto);

            return updatedGameDto;
        }
    }
}
