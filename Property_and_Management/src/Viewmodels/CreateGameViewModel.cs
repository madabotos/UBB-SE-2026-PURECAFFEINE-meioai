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
    public class CreateGameViewModel
    {
        private readonly IGameService _gameService;

        // UI Binding Properties
        public string Name { get; set; } = string.Empty;
        public double Price { get; set; }
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
            if (!ValidateInputs()) return null;

            if (Image == null || Image.Length == 0)
            {
                try
                {
                    // Reads the default image from your Assets folder and converts it to bytes
                    string defaultImagePath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Assets", "default-game-placeholder.png");
                    Image = System.IO.File.ReadAllBytes(defaultImagePath);
                }
                catch
                {
                    // If the default image is missing, just send an empty array so the app doesn't crash
                    Image = new byte[0];
                }
            }

            var newGameDto = new GameDTO(
                id: 0,
                owner: new User(CurrentUserId),
                name: Name,
                price: Price,
                minimumPlayerNumber: MinPlayers,
                maximumPlayerNumaber: MaxPlayers,
                description: Description,
                image: Image,
                isActive: IsActive
            );

            _gameService.AddGame(newGameDto);
            return newGameDto;
        }
    }
}
