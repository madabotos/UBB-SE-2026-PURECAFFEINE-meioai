using System;
using System.Collections.Immutable;
using System.Linq;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Service
{
    public class RentalService
    {
        private readonly IRentalRepository _rentalRepository;
        private readonly IGameRepository _gameRepository;

        public RentalService(IRentalRepository rentalRepository, IGameRepository gameRepository)
        {
            _rentalRepository = rentalRepository;
            _gameRepository = gameRepository;
        }

        // [ENT-REN-03] Validation for the 48-hour buffer period
        public bool IsSlotAvailable(int gameId, DateTime newStart, DateTime newEnd)
        {
            var existingRentals = _rentalRepository.GetRentalsByGame(gameId);

            foreach (var rental in existingRentals)
            {
                // Regula: start_date nu poate fi in intervalul [existent_start, existent_end + 48h]
                var bufferEnd = rental.EndDate.AddHours(48);

                if (newStart < bufferEnd && newEnd > rental.StartDate)
                {
                    return false; // Overlap detected with buffer
                }
            }
            return true;
        }

        public void CreateConfirmedRental(Rental rental)
        {
            // [ENT-REN-04] Validate that seller_id matches the game's owner
            var game = _gameRepository.Get(rental.Game.Id);
            if (game.Owner.Id != rental.Owner.Id)
            {
                throw new InvalidOperationException("Seller ID must match Game Owner ID [ENT-REN-04].");
            }

            // [ENT-REN-03] Re-verify availability before final commit
            if (!IsSlotAvailable(rental.Game.Id, rental.StartDate, rental.EndDate))
            {
                throw new Exception("Selected dates fall within the mandatory 48-hour buffer of another rental.");
            }

            _rentalRepository.Add(rental);
        }
    }
}
