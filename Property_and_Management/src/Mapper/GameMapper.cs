using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Mapper
{
    public class GameMapper : IMapper<Game, GameDTO>
    {
        private readonly IMapper<User, UserDTO> gameOwnerMapper;

        public GameMapper(IMapper<User, UserDTO> gameOwnerMapper)
        {
            this.gameOwnerMapper = gameOwnerMapper;
        }

        public GameDTO ToDTO(Game game)
        {
            if (game == null)
            {
                return null;
            }

            return new GameDTO
            {
                Id = game.Id,
                Owner = gameOwnerMapper.ToDTO(game.Owner),
                Name = game.Name,
                Price = game.Price,
                MinimumPlayerNumber = game.MinimumPlayerNumber,
                MaximumPlayerNumber = game.MaximumPlayerNumber,
                Description = game.Description,
                Image = game.Image,
                IsActive = game.IsActive
            };
        }

        public Game ToModel(GameDTO GameDTO)
        {
            if (GameDTO == null)
            {
                return null;
            }

            return new Game
            {
                Id = GameDTO.Id,
                Owner = gameOwnerMapper.ToModel(GameDTO.Owner),
                Name = GameDTO.Name,
                Price = GameDTO.Price,
                MinimumPlayerNumber = GameDTO.MinimumPlayerNumber,
                MaximumPlayerNumber = GameDTO.MaximumPlayerNumber,
                Description = GameDTO.Description,
                Image = GameDTO.Image,
                IsActive = GameDTO.IsActive
            };
        }
    }
}