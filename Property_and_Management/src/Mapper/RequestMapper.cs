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

        public RequestDTO ToDTO(Request request)
        {
            if (request == null) return null;

            return new RequestDTO
            {
                Id = request.Id,
                Game = _gameMapper.ToDTO(request.Game),
                Renter = _userMapper.ToDTO(request.Renter),
                Owner = _userMapper.ToDTO(request.Owner),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = request.Status,
                OfferingUser = request.OfferingUser != null ? _userMapper.ToDTO(request.OfferingUser) : null
            };
        }

        public Request ToModel(RequestDTO requestDto)
        {
            if (requestDto == null) return null;

            return new Request
            {
                Id = requestDto.Id,
                Game = _gameMapper.ToModel(requestDto.Game),
                Renter = _userMapper.ToModel(requestDto.Renter),
                Owner = _userMapper.ToModel(requestDto.Owner),
                StartDate = requestDto.StartDate,
                EndDate = requestDto.EndDate,
                Status = requestDto.Status,
                OfferingUser = requestDto.OfferingUser != null ? _userMapper.ToModel(requestDto.OfferingUser) : null
            };
        }
    }
}
