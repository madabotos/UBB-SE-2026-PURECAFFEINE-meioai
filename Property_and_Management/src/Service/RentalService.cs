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
        private readonly IMapper<Rental, RentalDataTransferObject> rentalMapper;

        private const int NewEntityIdentifier = 0;

        public RentalService(
            IRentalRepository rentalRepository,
            IGameRepository gameRepository,
            IMapper<Rental, RentalDataTransferObject> rentalMapper)
        {
            this.rentalRepository = rentalRepository;
            this.gameRepository = gameRepository;
            this.rentalMapper = rentalMapper;
        }

        public bool IsSlotAvailable(int gameIdentifier, DateTime newStart, DateTime newEnd)
        {
            foreach (var rental in rentalRepository.GetRentalsByGame(gameIdentifier))
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

        public void CreateConfirmedRental(int gameIdentifier, int renterIdentifier, int ownerIdentifier, DateTime startDate, DateTime endDate)
        {
            var game = gameRepository.Get(gameIdentifier);
            if (game.Owner.Identifier != ownerIdentifier)
            {
                throw new InvalidOperationException("Seller ID must match Game Owner ID [ENT-REN-04].");
            }

            if (!IsSlotAvailable(gameIdentifier, startDate, endDate))
            {
                throw new InvalidOperationException(
                    $"Selected dates fall within the mandatory {DomainConstants.RentalBufferHours}-hour buffer of another rental.");
            }

            var rental = new Rental(
                identifier: NewEntityIdentifier,
                game: new Game { Identifier = gameIdentifier },
                renter: new User { Identifier = renterIdentifier },
                owner: new User { Identifier = ownerIdentifier },
                startDate: startDate,
                endDate: endDate);

            rentalRepository.AddConfirmed(rental);
        }

        public ImmutableList<RentalDataTransferObject> GetRentalsForRenter(int renterIdentifier) =>
            rentalRepository
                .GetRentalsByRenter(renterIdentifier)
                .Select(rental => rentalMapper.ToDataTransferObject(rental))
                .ToImmutableList();

        public ImmutableList<RentalDataTransferObject> GetRentalsForOwner(int ownerIdentifier) =>
            rentalRepository
                .GetRentalsByOwner(ownerIdentifier)
                .Select(rental => rentalMapper.ToDataTransferObject(rental))
                .ToImmutableList();
    }
}


