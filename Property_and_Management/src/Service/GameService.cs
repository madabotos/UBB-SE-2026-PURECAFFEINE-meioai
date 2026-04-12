using System;
using System.Collections.Immutable;
using System.Linq;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Service
{
    public class GameService : IGameService
    {
        private readonly IGameRepository gameRepository;
        private readonly IRentalRepository rentalRepository;
        private readonly IMapper<Game, GameDataTransferObject> gameMapper;
        private readonly IRequestService requestService;
        private const int NoActiveOrUpcomingRentals = 0;
        private const int SingularRentalCount = 1;

        public GameService(
            IGameRepository gameRepository,
            IRentalRepository rentalRepository,
            IMapper<Game, GameDataTransferObject> gameMapper,
            IRequestService requestService)
        {
            this.gameRepository = gameRepository;
            this.rentalRepository = rentalRepository;
            this.gameMapper = gameMapper;
            this.requestService = requestService;
        }

        public void AddGame(GameDataTransferObject game)
        {
            gameRepository.Add(gameMapper.ToModel(game));
        }

        public void UpdateGameByIdentifier(int gameIdentifier, GameDataTransferObject game)
        {
            gameRepository.Update(gameIdentifier, gameMapper.ToModel(game));
        }

        public GameDataTransferObject DeleteGameByIdentifier(int gameIdentifier)
        {
            var rentals = rentalRepository.GetRentalsByGame(gameIdentifier);
            var now = DateTime.Now;
            var activeOrUpcomingRentalsCount = rentals.Count(rental => rental.EndDate >= now);
            if (activeOrUpcomingRentalsCount > NoActiveOrUpcomingRentals)
            {
                var rentalWord = activeOrUpcomingRentalsCount == SingularRentalCount ? "rental" : "rentals";
                throw new InvalidOperationException(
                    $"There are {activeOrUpcomingRentalsCount} active {rentalWord} for this game and it cannot be removed now.");
            }

            foreach (var rental in rentals)
            {
                rentalRepository.Delete(rental.Identifier);
            }

            // Deleting a game invalidates pending requests for that game.
            // Reuse the existing deactivation flow to notify renters and clean requests first.
            requestService.OnGameDeactivated(gameIdentifier);
            return gameMapper.ToDataTransferObject(gameRepository.Delete(gameIdentifier));
        }

        public GameDataTransferObject GetGameByIdentifier(int gameIdentifier)
        {
            return gameMapper.ToDataTransferObject(gameRepository.Get(gameIdentifier));
        }

        public ImmutableList<GameDataTransferObject> GetGamesForOwner(int ownerIdentifier)
        {
            return gameRepository
                .GetGamesByOwner(ownerIdentifier)
                .Select(game => gameMapper.ToDataTransferObject(game))
                .ToImmutableList();
        }

        public ImmutableList<GameDataTransferObject> GetAllGames()
        {
            return gameRepository
                .GetAll()
                .Select(game => gameMapper.ToDataTransferObject(game))
                .ToImmutableList();
        }
    }
}

