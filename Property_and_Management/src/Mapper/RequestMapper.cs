using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Mapper
{
    public class RequestMapper : IMapper<Request, RequestDataTransferObject>
    {
        private readonly IMapper<Game, GameDataTransferObject> gameMapper;
        private readonly IMapper<User, UserDataTransferObject> userMapper;

        public RequestMapper(IMapper<Game, GameDataTransferObject> gameMapper, IMapper<User, UserDataTransferObject> userMapper)
        {
            this.gameMapper = gameMapper;
            this.userMapper = userMapper;
        }

        public RequestDataTransferObject ToDataTransferObject(Request request)
        {
            if (request == null)
            {
                return null;
            }

            return new RequestDataTransferObject
            {
                Identifier = request.Identifier,
                Game = gameMapper.ToDataTransferObject(request.Game),
                Renter = userMapper.ToDataTransferObject(request.Renter),
                Owner = userMapper.ToDataTransferObject(request.Owner),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = request.Status,
                OfferingUser = request.OfferingUser != null ? userMapper.ToDataTransferObject(request.OfferingUser) : null
            };
        }

        public Request ToModel(RequestDataTransferObject requestDataTransferObject)
        {
            if (requestDataTransferObject == null)
            {
                return null;
            }

            return new Request
            {
                Identifier = requestDataTransferObject.Identifier,
                Game = gameMapper.ToModel(requestDataTransferObject.Game),
                Renter = userMapper.ToModel(requestDataTransferObject.Renter),
                Owner = userMapper.ToModel(requestDataTransferObject.Owner),
                StartDate = requestDataTransferObject.StartDate,
                EndDate = requestDataTransferObject.EndDate,
                Status = requestDataTransferObject.Status,
                OfferingUser = requestDataTransferObject.OfferingUser != null ? userMapper.ToModel(requestDataTransferObject.OfferingUser) : null
            };
        }
    }
}

