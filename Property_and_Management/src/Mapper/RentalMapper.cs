using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Mapper
{
    public class RentalMapper : IMapper<Rental, RentalDTO>
    {
        private readonly IMapper<Game, GameDTO> gameMapper;
        private readonly IMapper<User, UserDTO> userMapper;

        public RentalMapper(IMapper<Game, GameDTO> gameMapper, IMapper<User, UserDTO> userMapper)
        {
            this.gameMapper = gameMapper;
            this.userMapper = userMapper;
        }

        public RentalDTO ToDTO(Rental rental)
        {
            if (rental == null)
            {
                return null;
            }

            return new RentalDTO
            {
                Id = rental.Id,
                Game = gameMapper.ToDTO(rental.Game),
                Renter = userMapper.ToDTO(rental.Renter),
                Owner = userMapper.ToDTO(rental.Owner),
                StartDate = rental.StartDate,
                EndDate = rental.EndDate
            };
        }

        public Rental ToModel(RentalDTO RentalDTO)
        {
            if (RentalDTO == null)
            {
                return null;
            }

            return new Rental
            {
                Id = RentalDTO.Id,
                Game = gameMapper.ToModel(RentalDTO.Game),
                Renter = userMapper.ToModel(RentalDTO.Renter),
                Owner = userMapper.ToModel(RentalDTO.Owner),
                StartDate = RentalDTO.StartDate,
                EndDate = RentalDTO.EndDate
            };
        }
    }
}