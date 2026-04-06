using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;
using Property_and_Management.src.Repository;

namespace Property_and_Management.src.Service
{
    public class GameService : IGameService
    {
        private readonly IGameRepository _gameRepository;
        private readonly IMapper<Game, GameDTO> _gameMapper;

        public GameService(IGameRepository gameRepository, IMapper<Game, GameDTO> gameMapper)
        {
            _gameRepository = gameRepository;
            _gameMapper = gameMapper;
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
    }
}
