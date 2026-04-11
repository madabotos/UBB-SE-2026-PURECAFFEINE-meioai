using System;
using System.Collections.Immutable;
using System.Linq;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Service
{
    public class RentalService : IRentalService
    {
        private readonly IRentalRepository _rentalRepository;
        private readonly IGameRepository _gameRepository;
        private readonly IMapper<Rental, RentalDTO> _rentalMapper;

        private const int BufferHours = 48;

        public RentalService(
            IRentalRepository rentalRepository,
            IGameRepository gameRepository,
            IMapper<Rental, RentalDTO> rentalMapper)
        {
            _rentalRepository = rentalRepository;
            _gameRepository = gameRepository;
            _rentalMapper = rentalMapper;
        }

        public bool IsSlotAvailable(int gameId, DateTime newStart, DateTime newEnd)
        {
            foreach (var rental in _rentalRepository.GetRentalsByGame(gameId))
            {
                var bufferStart = rental.StartDate.AddHours(-BufferHours);
                var bufferEnd = rental.EndDate.AddHours(BufferHours);
                if (newStart < bufferEnd && newEnd > bufferStart)
                    return false;
            }
            return true;
        }

        public void CreateConfirmedRental(int gameId, int renterId, int ownerId, DateTime startDate, DateTime endDate)
        {
            var game = _gameRepository.Get(gameId);
            if (game.Owner.Id != ownerId)
                throw new InvalidOperationException("Seller ID must match Game Owner ID [ENT-REN-04].");

            var rental = new Rental(
                id: 0,
                game: new Game { Id = gameId },
                renter: new User { Id = renterId },
                owner: new User { Id = ownerId },
                startDate: startDate,
                endDate: endDate);

            _rentalRepository.AddConfirmed(rental);
        }

        public ImmutableList<RentalDTO> GetRentalsForRenter(int renterId) =>
            _rentalRepository
                .GetRentalsByRenter(renterId)
                .Select(r => _rentalMapper.ToDTO(r))
                .ToImmutableList();

        public ImmutableList<RentalDTO> GetRentalsForOwner(int ownerId) =>
            _rentalRepository
                .GetRentalsByOwner(ownerId)
                .Select(r => _rentalMapper.ToDTO(r))
                .ToImmutableList();
    }
}
