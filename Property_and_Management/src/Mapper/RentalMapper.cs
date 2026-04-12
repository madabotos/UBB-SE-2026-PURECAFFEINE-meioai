using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Mapper
{
    public class RentalMapper : IMapper<Rental, RentalDataTransferObject>
    {
        private readonly IMapper<Game, GameDataTransferObject> gameMapper;
        private readonly IMapper<User, UserDataTransferObject> userMapper;

        public RentalMapper(IMapper<Game, GameDataTransferObject> gameMapper, IMapper<User, UserDataTransferObject> userMapper)
        {
            this.gameMapper = gameMapper;
            this.userMapper = userMapper;
        }

        public RentalDataTransferObject ToDataTransferObject(Rental rental)
        {
            if (rental == null)
            {
                return null;
            }

            return new RentalDataTransferObject
            {
                Identifier = rental.Identifier,
                Game = gameMapper.ToDataTransferObject(rental.Game),
                Renter = userMapper.ToDataTransferObject(rental.Renter),
                Owner = userMapper.ToDataTransferObject(rental.Owner),
                StartDate = rental.StartDate,
                EndDate = rental.EndDate
            };
        }

        public Rental ToModel(RentalDataTransferObject rentalDataTransferObject)
        {
            if (rentalDataTransferObject == null)
            {
                return null;
            }

            return new Rental
            {
                Identifier = rentalDataTransferObject.Identifier,
                Game = gameMapper.ToModel(rentalDataTransferObject.Game),
                Renter = userMapper.ToModel(rentalDataTransferObject.Renter),
                Owner = userMapper.ToModel(rentalDataTransferObject.Owner),
                StartDate = rentalDataTransferObject.StartDate,
                EndDate = rentalDataTransferObject.EndDate
            };
        }
    }
}

