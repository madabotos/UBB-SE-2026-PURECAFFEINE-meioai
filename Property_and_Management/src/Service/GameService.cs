using System;
using System.Collections.Immutable;
using System.Linq;
using Property_and_Management.src.DataTransferObjects;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Service
{
    public class GameService : IGameService
    {
        private readonly IGameRepository _gameRepository;
        private readonly IRentalRepository _rentalRepository;
        private readonly IMapper<Game, GameDataTransferObject> _gameMapper;
        private readonly IRequestService _requestService;
        private const int NoActiveOrUpcomingRentals = 0;
        private const int SingularRentalCount = 1;

        public GameService(
            IGameRepository gameRepository,
            IRentalRepository rentalRepository,
            IMapper<Game, GameDataTransferObject> gameMapper,
            IRequestService requestService)
        {
            _gameRepository = gameRepository;
            _rentalRepository = rentalRepository;
            _gameMapper = gameMapper;
            _requestService = requestService;
        }

        public void AddGame(GameDataTransferObject game)
        {
            _gameRepository.Add(_gameMapper.ToModel(game));
        }

        public void UpdateGameByIdentifier(int gameIdentifier, GameDataTransferObject game)
        {
            _gameRepository.Update(gameIdentifier, _gameMapper.ToModel(game));
        }

        public GameDataTransferObject DeleteGameByIdentifier(int gameIdentifier)
        {
            var rentals = _rentalRepository.GetRentalsByGame(gameIdentifier);
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
                _rentalRepository.Delete(rental.Identifier);
            }

            // Deleting a game invalidates pending requests for that game.
            // Reuse the existing deactivation flow to notify renters and clean requests first.
            _requestService.OnGameDeactivated(gameIdentifier);
            return _gameMapper.ToDataTransferObject(_gameRepository.Delete(gameIdentifier));
        }

        public GameDataTransferObject GetGameByIdentifier(int gameIdentifier)
        {
            return _gameMapper.ToDataTransferObject(_gameRepository.Get(gameIdentifier));
        }

        public ImmutableList<GameDataTransferObject> GetGamesForOwner(int ownerIdentifier)
        {
            return _gameRepository
                .GetGamesByOwner(ownerIdentifier)
                .Select(game => _gameMapper.ToDataTransferObject(game))
                .ToImmutableList();
        }

        public ImmutableList<GameDataTransferObject> GetAllGames()
        {
            return _gameRepository
                .GetAll()
                .Select(game => _gameMapper.ToDataTransferObject(game))
                .ToImmutableList();
        }
    }
}

