using Property_and_Management.src.DataTransferObjects;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Mapper
{
    public class GameMapper : IMapper<Game, GameDataTransferObject>
    {
        private readonly IMapper<User, UserDataTransferObject> _userMapper;

        public GameMapper(IMapper<User, UserDataTransferObject> userMapper)
        {
            _userMapper = userMapper;
        }

        public GameDataTransferObject ToDataTransferObject(Game game)
        {
            if (game == null) return null;

            return new GameDataTransferObject
            {
                Identifier = game.Identifier,
                Owner = _userMapper.ToDataTransferObject(game.Owner),
                Name = game.Name,
                Price = game.Price,
                MinimumPlayerNumber = game.MinimumPlayerNumber,
                MaximumPlayerNumber = game.MaximumPlayerNumber,
                Description = game.Description,
                Image = game.Image,
                IsActive = game.IsActive
            };
        }

        public Game ToModel(GameDataTransferObject gameDataTransferObject)
        {
            if (gameDataTransferObject == null) return null;

            return new Game
            {
                Identifier = gameDataTransferObject.Identifier,
                Owner = _userMapper.ToModel(gameDataTransferObject.Owner),
                Name = gameDataTransferObject.Name,
                Price = gameDataTransferObject.Price,
                MinimumPlayerNumber = gameDataTransferObject.MinimumPlayerNumber,
                MaximumPlayerNumber = gameDataTransferObject.MaximumPlayerNumber,
                Description = gameDataTransferObject.Description,
                Image = gameDataTransferObject.Image,
                IsActive = gameDataTransferObject.IsActive
            };
        }
    }
}

