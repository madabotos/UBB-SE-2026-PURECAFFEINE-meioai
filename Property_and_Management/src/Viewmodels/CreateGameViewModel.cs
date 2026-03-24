using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Property_and_Management.src.Service;
using Property_and_Management.src.Model;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Repository;

namespace Property_and_Management.src.Viewmodels
{
    public class CreateGameViewModel
    {
        private readonly IGameService _gameService;

        // UI Binding Properties
        public string Name { get; set; } = string.Empty;
        public double Price { get; set; }

        // This binds to the ComboBox SelectedIndex in the UI (0 = Per Hour, 1 = Per Day, 2 = Per Week)
        public int RateTypeIndex { get; set; } = 0;

        public int MinPlayers { get; set; } = 1;
        public int MaxPlayers { get; set; } = 4;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public byte[] Image { get; set; } = null; // Optional picture upload

        // MOCK USER: Hardcoded to simulate being logged in as the owner 
        public int CurrentUserId { get; set; } = 1;

        public CreateGameViewModel()
        {
            // Instantiate the repository and inject it into the service
            IGameRepository gameRepository = new GameRepository();
            _gameService = new GameService(gameRepository);
        }

        // [UI-CRG-03] The system shall validate all form inputs against the constraints 
        public bool ValidateInputs()
        {
            // Check Name constraints: 5-30 characters 
            if (string.IsNullOrWhiteSpace(Name) || Name.Length < 5 || Name.Length > 30)
                return false;

            // Check Price constraints: > 0 
            if (Price <= 0)
                return false;

            // Check Player constraints: >= 1 and Max >= Min 
            if (MinPlayers < 1)
                return false;
            if (MaxPlayers < MinPlayers)
                return false;

            // Check Description constraints: 10-500 characters 
            if (string.IsNullOrWhiteSpace(Description) || Description.Length < 10 || Description.Length > 500)
                return false;

            return true;
        }

        // Creates the GameDTO, sends it to the Service, and returns it (as required by UML) 
        public GameDTO SaveGame()
        {
            if (!ValidateInputs())
            {
                // Safety check: Don't save if validation somehow failed
                return null;
            }

            // Map the UI ComboBox index (0, 1, 2) to the expected RateType Enum values (1, 2, 3) 
            // Assuming your RateType enum maps as: 1 = PER_HOUR, 2 = PER_DAY, 3 = PER_WEEK 
            int rateTypeValue = RateTypeIndex + 1;

            // Create the DTO to pass to the service 
            var newGameDto = new GameDTO(
                id: 0, // DB will auto-generate this 
                ownerId: CurrentUserId, // Links to the current user 
                name: Name,
                price: Price,
                rateType: (RateType)rateTypeValue, // Cast to your RateType enum
                minimumPlayerNumber: MinPlayers,
                maximumPlayerNumber: MaxPlayers,
                description: Description,
                image: Image,
                isActive: IsActive
            );

            // Send to the GameService you provided to insert into the database
            _gameService.AddGame(newGameDto);

            return newGameDto;
        }
    }
}
