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
        private readonly IMapper<Game, GameDTO> gameMapper;
        private readonly IRequestService requestService;
        private const int NoActiveOrUpcomingRentals = 0;
        private const int SingularRentalCount = 1;

        public GameService(
            IGameRepository gameRepository,
            IRentalRepository rentalRepository,
            IMapper<Game, GameDTO> gameMapper,
            IRequestService requestService)
        {
            this.gameRepository = gameRepository;
            this.rentalRepository = rentalRepository;
            this.gameMapper = gameMapper;
            this.requestService = requestService;
        }

        public void AddGame(GameDTO game)
        {
            gameRepository.Add(gameMapper.ToModel(game));
        }

        public void UpdateGameByIdentifier(int gameId, GameDTO game)
        {
            gameRepository.Update(gameId, gameMapper.ToModel(game));
        }

        public GameDTO DeleteGameByIdentifier(int gameId)
        {
            var rentals = rentalRepository.GetRentalsByGame(gameId);
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
                rentalRepository.Delete(rental.Id);
            }

            requestService.OnGameDeactivated(gameId);
            return gameMapper.ToDTO(gameRepository.Delete(gameId));
        }

        public GameDTO GetGameByIdentifier(int gameId)
        {
            return gameMapper.ToDTO(gameRepository.Get(gameId));
        }

        public ImmutableList<GameDTO> GetGamesForOwner(int ownerId)
        {
            return gameRepository
                .GetGamesByOwner(ownerId)
                .Select(game => gameMapper.ToDTO(game))
                .ToImmutableList();
        }

        public ImmutableList<GameDTO> GetAllGames()
        {
            return gameRepository
                .GetAll()
                .Select(game => gameMapper.ToDTO(game))
                .ToImmutableList();
        }
    }
}