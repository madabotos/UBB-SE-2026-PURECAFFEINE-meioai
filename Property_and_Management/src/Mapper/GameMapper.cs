using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Mapper
{
    public class GameMapper : IMapper<Game, GameDTO>
    {
        private readonly IMapper<User, UserDTO> _userMapper;

        public GameMapper(IMapper<User, UserDTO> userMapper)
        {
            _userMapper = userMapper;
        }

        public GameDTO ToDTO(Game game)
        {
            if (game == null) return null;

            return new GameDTO
            {
                Id = game.Id,
                Owner = _userMapper.ToDTO(game.Owner),
                Name = game.Name,
                Price = game.Price,
                MinimumPlayerNumber = game.MinimumPlayerNumber,
                MaximumPlayerNumber = game.MaximumPlayerNumber,
                Description = game.Description,
                Image = game.Image,
                IsActive = game.IsActive
            };
        }

        public Game ToModel(GameDTO gameDto)
        {
            if (gameDto == null) return null;

            return new Game
            {
                Id = gameDto.Id,
                Owner = _userMapper.ToModel(gameDto.Owner),
                Name = gameDto.Name,
                Price = gameDto.Price,
                MinimumPlayerNumber = gameDto.MinimumPlayerNumber,
                MaximumPlayerNumber = gameDto.MaximumPlayerNumber,
                Description = gameDto.Description,
                Image = gameDto.Image,
                IsActive = gameDto.IsActive
            };
        }
    }
}
