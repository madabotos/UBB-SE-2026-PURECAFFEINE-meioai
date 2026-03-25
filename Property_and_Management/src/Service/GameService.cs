using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;
using Property_and_Management.src.Repository;

namespace Property_and_Management.src.Service
{
    public class GameService : IGameService
    {
        private IGameRepository _gameRepository;

        public GameService(IGameRepository gameRepository)
        {
            _gameRepository = gameRepository;
        }

        public void SetGameRepository(IGameRepository gameRepository) =>
            _gameRepository = gameRepository;

        public void AddGame(GameDTO game)
        {
            _gameRepository.Add(game.ToModel());
        }

        public void UpdateGameById(int id, GameDTO game)
        {
            _gameRepository.Update(id, game.ToModel());
        }

        public GameDTO DeleteGameById(int id)
        {
            return (GameDTO)GameDTO.FromModel(_gameRepository.Delete(id));
        }

        public GameDTO GetGameById(int id)
        {
            return (GameDTO)GameDTO.FromModel(_gameRepository.Get(id));
        }

        public ImmutableList<GameDTO> GetGamesForOwner(int ownerId)
        {
            return _gameRepository
                .GetGamesByOwner(ownerId)
                .Select(game => new GameDTO(game))
                .ToImmutableList();
        }
    }
}
