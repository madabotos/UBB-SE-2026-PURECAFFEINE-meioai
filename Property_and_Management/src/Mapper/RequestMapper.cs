using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Mapper
{
    public class RequestMapper : IMapper<Request, RequestDTO>
    {
        private readonly IMapper<Game, GameDTO> gameMapper;
        private readonly IMapper<User, UserDTO> userMapper;

        public RequestMapper(IMapper<Game, GameDTO> gameMapper, IMapper<User, UserDTO> userMapper)
        {
            this.gameMapper = gameMapper;
            this.userMapper = userMapper;
        }

        public RequestDTO ToDTO(Request request)
        {
            if (request == null)
            {
                return null;
            }

            return new RequestDTO
            {
                Id = request.Id,
                Game = gameMapper.ToDTO(request.Game),
                Renter = userMapper.ToDTO(request.Renter),
                Owner = userMapper.ToDTO(request.Owner),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = request.Status,
                OfferingUser = request.OfferingUser != null ? userMapper.ToDTO(request.OfferingUser) : null
            };
        }

        public Request ToModel(RequestDTO RequestDTO)
        {
            if (RequestDTO == null)
            {
                return null;
            }

            return new Request
            {
                Id = RequestDTO.Id,
                Game = gameMapper.ToModel(RequestDTO.Game),
                Renter = userMapper.ToModel(RequestDTO.Renter),
                Owner = userMapper.ToModel(RequestDTO.Owner),
                StartDate = RequestDTO.StartDate,
                EndDate = RequestDTO.EndDate,
                Status = RequestDTO.Status,
                OfferingUser = RequestDTO.OfferingUser != null ? userMapper.ToModel(RequestDTO.OfferingUser) : null
            };
        }
    }
}