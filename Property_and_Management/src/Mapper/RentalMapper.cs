using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Mapper
{
    public class RentalMapper : IMapper<Rental, RentalDTO>
    {
        private readonly IMapper<Game, GameDTO> _gameMapper;
        private readonly IMapper<User, UserDTO> _userMapper;

        public RentalMapper(IMapper<Game, GameDTO> gameMapper, IMapper<User, UserDTO> userMapper)
        {
            _gameMapper = gameMapper;
            _userMapper = userMapper;
        }

        public RentalDTO ToDTO(Rental entity)
        {
            if (entity == null) return null;

            return new RentalDTO
            {
                Id = entity.Id,
                Game = _gameMapper.ToDTO(entity.Game),
                Renter = _userMapper.ToDTO(entity.Renter),
                Owner = _userMapper.ToDTO(entity.Owner),
                StartDate = entity.StartDate,
                EndDate = entity.EndDate
            };
        }

        public Rental ToModel(RentalDTO dto)
        {
            if (dto == null) return null;

            return new Rental
            {
                Id = dto.Id,
                Game = _gameMapper.ToModel(dto.Game),
                Renter = _userMapper.ToModel(dto.Renter),
                Owner = _userMapper.ToModel(dto.Owner),
                StartDate = dto.StartDate,
                EndDate = dto.EndDate
            };
        }
    }
}
