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

        public RentalDTO ToDTO(Rental rental)
        {
            if (rental == null) return null;

            return new RentalDTO
            {
                Id = rental.Id,
                Game = _gameMapper.ToDTO(rental.Game),
                Renter = _userMapper.ToDTO(rental.Renter),
                Owner = _userMapper.ToDTO(rental.Owner),
                StartDate = rental.StartDate,
                EndDate = rental.EndDate
            };
        }

        public Rental ToModel(RentalDTO rentalDto)
        {
            if (rentalDto == null) return null;

            return new Rental
            {
                Id = rentalDto.Id,
                Game = _gameMapper.ToModel(rentalDto.Game),
                Renter = _userMapper.ToModel(rentalDto.Renter),
                Owner = _userMapper.ToModel(rentalDto.Owner),
                StartDate = rentalDto.StartDate,
                EndDate = rentalDto.EndDate
            };
        }
    }
}
