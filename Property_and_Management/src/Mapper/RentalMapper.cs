using Property_and_Management.src.DataTransferObjects;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Mapper
{
    public class RentalMapper : IMapper<Rental, RentalDataTransferObject>
    {
        private readonly IMapper<Game, GameDataTransferObject> _gameMapper;
        private readonly IMapper<User, UserDataTransferObject> _userMapper;

        public RentalMapper(IMapper<Game, GameDataTransferObject> gameMapper, IMapper<User, UserDataTransferObject> userMapper)
        {
            _gameMapper = gameMapper;
            _userMapper = userMapper;
        }

        public RentalDataTransferObject ToDataTransferObject(Rental rental)
        {
            if (rental == null) return null;

            return new RentalDataTransferObject
            {
                Id = rental.Id,
                Game = _gameMapper.ToDataTransferObject(rental.Game),
                Renter = _userMapper.ToDataTransferObject(rental.Renter),
                Owner = _userMapper.ToDataTransferObject(rental.Owner),
                StartDate = rental.StartDate,
                EndDate = rental.EndDate
            };
        }

        public Rental ToModel(RentalDataTransferObject rentalDataTransferObject)
        {
            if (rentalDataTransferObject == null) return null;

            return new Rental
            {
                Id = rentalDataTransferObject.Id,
                Game = _gameMapper.ToModel(rentalDataTransferObject.Game),
                Renter = _userMapper.ToModel(rentalDataTransferObject.Renter),
                Owner = _userMapper.ToModel(rentalDataTransferObject.Owner),
                StartDate = rentalDataTransferObject.StartDate,
                EndDate = rentalDataTransferObject.EndDate
            };
        }
    }
}
