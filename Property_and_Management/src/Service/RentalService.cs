using System;
using System.Collections.Immutable;
using System.Linq;
using Property_and_Management.Src.Constants;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Service
{
    public class RentalService : IRentalService
    {
        private readonly IRentalRepository rentalRepository;
        private readonly IGameRepository gameRepository;
        private readonly IMapper<Rental, RentalDTO> rentalMapper;

        private const int NewEntityId = 0;

        public RentalService(
            IRentalRepository rentalRepository,
            IGameRepository gameRepository,
            IMapper<Rental, RentalDTO> rentalMapper)
        {
            this.rentalRepository = rentalRepository;
            this.gameRepository = gameRepository;
            this.rentalMapper = rentalMapper;
        }

        public bool IsSlotAvailable(int gameId, DateTime newStart, DateTime newEnd)
        {
            foreach (var rental in rentalRepository.GetRentalsByGame(gameId))
            {
                var bufferStart = rental.StartDate.AddHours(-DomainConstants.RentalBufferHours);
                var bufferEnd = rental.EndDate.AddHours(DomainConstants.RentalBufferHours);
                if (newStart < bufferEnd && newEnd > bufferStart)
                {
                    return false;
                }
            }

            return true;
        }

        public void CreateConfirmedRental(int gameId, int renterId, int ownerId, DateTime startDate, DateTime endDate)
        {
            var game = gameRepository.Get(gameId);
            if (game.Owner.Id != ownerId)
            {
                throw new InvalidOperationException("Seller ID must match Game Owner ID [ENT-REN-04].");
            }

            if (!IsSlotAvailable(gameId, startDate, endDate))
            {
                throw new InvalidOperationException(
                    $"Selected dates fall within the mandatory {DomainConstants.RentalBufferHours}-hour buffer of another rental.");
            }

            var rental = new Rental(
                id: NewEntityId,
                game: new Game { Id = gameId },
                renter: new User { Id = renterId },
                owner: new User { Id = ownerId },
                startDate: startDate,
                endDate: endDate);

            rentalRepository.AddConfirmed(rental);
        }

        public ImmutableList<RentalDTO> GetRentalsForRenter(int renterId) =>
            rentalRepository
                .GetRentalsByRenter(renterId)
                .Select(rental => rentalMapper.ToDTO(rental))
                .ToImmutableList();

        public ImmutableList<RentalDTO> GetRentalsForOwner(int ownerId) =>
            rentalRepository
                .GetRentalsByOwner(ownerId)
                .Select(rental => rentalMapper.ToDTO(rental))
                .ToImmutableList();
    }
}