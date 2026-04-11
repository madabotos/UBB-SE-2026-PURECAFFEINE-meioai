using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Mapper
{
    public class GameMapper : IMapper<Game, GameDataTransferObject>
    {
        private readonly IMapper<User, UserDataTransferObject> userMapper;

        public GameMapper(IMapper<User, UserDataTransferObject> userMapper)
        {
            this.userMapper = userMapper;
        }

        public GameDataTransferObject ToDataTransferObject(Game game)
        {
            if (game == null)
            {
                return null;
            }

            return new GameDataTransferObject
            {
                Identifier = game.Identifier,
                Owner = userMapper.ToDataTransferObject(game.Owner),
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
            if (gameDataTransferObject == null)
            {
                return null;
            }

            return new Game
            {
                Identifier = gameDataTransferObject.Identifier,
                Owner = userMapper.ToModel(gameDataTransferObject.Owner),
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

