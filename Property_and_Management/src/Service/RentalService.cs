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
            var existingRentals = _rentalRepository.GetRentalsByGame(gameId);
            const int bufferHours = 48;

            foreach (var rental in existingRentals)
            {
                var bufferStart = rental.StartDate.AddHours(-bufferHours);
                var bufferEnd = rental.EndDate.AddHours(bufferHours);
                if (newStart < bufferEnd && newEnd > bufferStart)
                    return false;
            }
            return true;
        }

        public void CreateConfirmedRental(Rental rental)
        {
            var game = _gameRepository.Get(rental.Game.Id);
            if (game.Owner.Id != rental.Owner.Id)
                throw new InvalidOperationException("Seller ID must match Game Owner ID [ENT-REN-04].");

            if (!IsSlotAvailable(rental.Game.Id, rental.StartDate, rental.EndDate))
                throw new Exception("Selected dates fall within the mandatory 48-hour buffer of another rental.");

            _rentalRepository.Add(rental);
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
