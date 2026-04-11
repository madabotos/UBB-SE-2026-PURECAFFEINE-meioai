using System;
using System.Collections.Immutable;
using System.Linq;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Service
{
    public class GameService : IGameService
    {
        private readonly IGameRepository _gameRepository;
        private readonly IRentalRepository _rentalRepository;
        private readonly IMapper<Game, GameDTO> _gameMapper;
        private readonly IRequestService _requestService;
        private const int NoActiveOrUpcomingRentals = 0;
        private const int SingularRentalCount = 1;

        public GameService(
            IGameRepository gameRepository,
            IRentalRepository rentalRepository,
            IMapper<Game, GameDTO> gameMapper,
            IRequestService requestService)
        {
            _gameRepository = gameRepository;
            _rentalRepository = rentalRepository;
            _gameMapper = gameMapper;
            _requestService = requestService;
        }

        public void AddGame(GameDTO game)
        {
            _gameRepository.Add(_gameMapper.ToModel(game));
        }

        public void UpdateGameById(int id, GameDTO game)
        {
            _gameRepository.Update(id, _gameMapper.ToModel(game));
        }

        public GameDTO DeleteGameById(int id)
        {
            var rentals = _rentalRepository.GetRentalsByGame(id);
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
                _rentalRepository.Delete(rental.Id);
            }

            // Deleting a game invalidates pending requests for that game.
            // Reuse the existing deactivation flow to notify renters and clean requests first.
            _requestService.OnGameDeactivated(id);
            return _gameMapper.ToDTO(_gameRepository.Delete(id));
        }

        public GameDTO GetGameById(int id)
        {
            return _gameMapper.ToDTO(_gameRepository.Get(id));
        }

        public ImmutableList<GameDTO> GetGamesForOwner(int ownerId)
        {
            return _gameRepository
                .GetGamesByOwner(ownerId)
                .Select(game => _gameMapper.ToDTO(game))
                .ToImmutableList();
        }

        public ImmutableList<GameDTO> GetAllGames()
        {
            return _gameRepository
                .GetAll()
                .Select(game => _gameMapper.ToDTO(game))
                .ToImmutableList();
        }
    }
}
