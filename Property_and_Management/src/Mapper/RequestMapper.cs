using Property_and_Management.src.DataTransferObjects;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Mapper
{
    public class RequestMapper : IMapper<Request, RequestDataTransferObject>
    {
        private readonly IMapper<Game, GameDataTransferObject> _gameMapper;
        private readonly IMapper<User, UserDataTransferObject> _userMapper;

        public RequestMapper(IMapper<Game, GameDataTransferObject> gameMapper, IMapper<User, UserDataTransferObject> userMapper)
        {
            _gameMapper = gameMapper;
            _userMapper = userMapper;
        }

        public RequestDataTransferObject ToDataTransferObject(Request request)
        {
            if (request == null) return null;

            return new RequestDataTransferObject
            {
                Id = request.Id,
                Game = _gameMapper.ToDataTransferObject(request.Game),
                Renter = _userMapper.ToDataTransferObject(request.Renter),
                Owner = _userMapper.ToDataTransferObject(request.Owner),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = request.Status,
                OfferingUser = request.OfferingUser != null ? _userMapper.ToDataTransferObject(request.OfferingUser) : null
            };
        }

        public Request ToModel(RequestDataTransferObject requestDataTransferObject)
        {
            if (requestDataTransferObject == null) return null;

            return new Request
            {
                Id = requestDataTransferObject.Id,
                Game = _gameMapper.ToModel(requestDataTransferObject.Game),
                Renter = _userMapper.ToModel(requestDataTransferObject.Renter),
                Owner = _userMapper.ToModel(requestDataTransferObject.Owner),
                StartDate = requestDataTransferObject.StartDate,
                EndDate = requestDataTransferObject.EndDate,
                Status = requestDataTransferObject.Status,
                OfferingUser = requestDataTransferObject.OfferingUser != null ? _userMapper.ToModel(requestDataTransferObject.OfferingUser) : null
            };
        }
    }
}
