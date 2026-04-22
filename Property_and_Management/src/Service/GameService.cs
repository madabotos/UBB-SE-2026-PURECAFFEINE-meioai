using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Property_and_Management.Src.Constants;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Service
{
    public class GameService : IGameService
    {
        private readonly IGameRepository gameListingRepository;
        private readonly IRentalRepository gameRentalRepository;
        private readonly IMapper<Game, GameDTO> gameDtoMapper;
        private readonly IRequestService rentalRequestService;
        private const int NoActiveOrUpcomingRentals = 0;
        private const int SingularRentalCount = 1;

        public GameService(
            IGameRepository gameRepository,
            IRentalRepository rentalRepository,
            IMapper<Game, GameDTO> gameMapper,
            IRequestService requestService)
        {
            this.gameListingRepository = gameRepository;
            this.gameRentalRepository = rentalRepository;
            this.gameDtoMapper = gameMapper;
            this.rentalRequestService = requestService;
        }

        public List<string> ValidateGame(GameDTO gameDto)
        {
            return GameInputHelper.BuildValidationErrors(
                gameDto.Name,
                gameDto.Price,
                gameDto.MinimumPlayerNumber,
                gameDto.MaximumPlayerNumber,
                gameDto.Description,
                DomainConstants.GameMinimumNameLength,
                DomainConstants.GameMaximumNameLength,
                DomainConstants.GameMinimumAllowedPrice,
                DomainConstants.GameMinimumPlayerCount,
                DomainConstants.GameMinimumDescriptionLength,
                DomainConstants.GameMaximumDescriptionLength);
        }

        public void AddGame(GameDTO gameToAdd)
        {
            var validationErrors = ValidateGame(gameToAdd);
            if (validationErrors.Count > NoActiveOrUpcomingRentals)
            {
                throw new ArgumentException(string.Join(Environment.NewLine, validationErrors));
            }

            gameToAdd.Image = GameInputHelper.EnsureImageOrDefault(gameToAdd.Image, AppDomain.CurrentDomain.BaseDirectory);
            gameListingRepository.Add(gameDtoMapper.ToModel(gameToAdd));
        }

        public void UpdateGameByIdentifier(int gameId, GameDTO updatedGameData)
        {
            var validationErrors = ValidateGame(updatedGameData);
            if (validationErrors.Count > NoActiveOrUpcomingRentals)
            {
                throw new ArgumentException(string.Join(Environment.NewLine, validationErrors));
            }

            updatedGameData.Image = GameInputHelper.EnsureImageOrDefault(updatedGameData.Image, AppDomain.CurrentDomain.BaseDirectory);
            gameListingRepository.Update(gameId, gameDtoMapper.ToModel(updatedGameData));
        }

        public GameDTO DeleteGameByIdentifier(int gameId)
        {
            var gameRentals = gameRentalRepository.GetRentalsByGame(gameId);
            var currentTime = DateTime.Now;
            var activeOrUpcomingRentalsCount = gameRentals.Count(rental => rental.EndDate >= currentTime);
            if (activeOrUpcomingRentalsCount > NoActiveOrUpcomingRentals)
            {
                var rentalWord = activeOrUpcomingRentalsCount == SingularRentalCount ? "rental" : "rentals";
                throw new InvalidOperationException(
                    $"There are {activeOrUpcomingRentalsCount} active {rentalWord} for this game and it cannot be removed now.");
            }

            foreach (var pastRental in gameRentals)
            {
                gameRentalRepository.Delete(pastRental.Id);
            }

            rentalRequestService.OnGameDeactivated(gameId);
            return gameDtoMapper.ToDTO(gameListingRepository.Delete(gameId));
        }

        public GameDTO GetGameByIdentifier(int gameId)
        {
            return gameDtoMapper.ToDTO(gameListingRepository.Get(gameId));
        }

        public ImmutableList<GameDTO> GetGamesForOwner(int ownerUserId)
        {
            return gameListingRepository
                .GetGamesByOwner(ownerUserId)
                .Select(game => gameDtoMapper.ToDTO(game))
                .ToImmutableList();
        }

        public ImmutableList<GameDTO> GetAllGames()
        {
            return gameListingRepository
                .GetAll()
                .Select(game => gameDtoMapper.ToDTO(game))
                .ToImmutableList();
        }

        public ImmutableList<GameDTO> GetAvailableGamesForRenter(int renterUserId)
        {
            return GetAllGames()
                .Where(game => game.IsActive && game.Owner?.Id != renterUserId)
                .ToImmutableList();
        }

        public ImmutableList<GameDTO> GetActiveGamesForOwner(int ownerUserId)
        {
            return GetGamesForOwner(ownerUserId)
                .Where(game => game.IsActive)
                .ToImmutableList();
        }
    }
}
