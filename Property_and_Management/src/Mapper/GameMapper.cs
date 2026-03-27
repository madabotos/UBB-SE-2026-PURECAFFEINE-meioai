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

        public GameDTO ToDTO(Game entity)
        {
            if (entity == null) return null;

            return new GameDTO
            {
                Id = entity.Id,
                Owner = _userMapper.ToDTO(entity.Owner),
                Name = entity.Name,
                Price = entity.Price,
                MinimumPlayerNumber = entity.MinimumPlayerNumber,
                MaximumPlayerNumber = entity.MaximumPlayerNumber,
                Description = entity.Description,
                Image = entity.Image,
                IsActive = entity.IsActive
            };
        }

        public Game ToModel(GameDTO dto)
        {
            if (dto == null) return null;

            return new Game
            {
                Id = dto.Id,
                Owner = _userMapper.ToModel(dto.Owner),
                Name = dto.Name,
                Price = dto.Price,
                MinimumPlayerNumber = dto.MinimumPlayerNumber,
                MaximumPlayerNumber = dto.MaximumPlayerNumber,
                Description = dto.Description,
                Image = dto.Image,
                IsActive = dto.IsActive
            };
        }
    }
}
