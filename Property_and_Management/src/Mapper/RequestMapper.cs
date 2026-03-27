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
    public class RequestMapper : IMapper<Request, RequestDTO>
    {
        private readonly IMapper<Game, GameDTO> _gameMapper;
        private readonly IMapper<User, UserDTO> _userMapper;

        public RequestMapper(IMapper<Game, GameDTO> gameMapper, IMapper<User, UserDTO> userMapper)
        {
            _gameMapper = gameMapper;
            _userMapper = userMapper;
        }

        public RequestDTO ToDTO(Request entity)
        {
            if (entity == null) return null;

            return new RequestDTO
            {
                Id = entity.Id,
                Game = _gameMapper.ToDTO(entity.Game),
                Renter = _userMapper.ToDTO(entity.Renter),
                Owner = _userMapper.ToDTO(entity.Owner),
                StartDate = entity.StartDate,
                EndDate = entity.EndDate
            };
        }

        public Request ToModel(RequestDTO dto)
        {
            if (dto == null) return null;

            return new Request
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
