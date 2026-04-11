using System;
using System.Collections.Immutable;
using Property_and_Management.src.DataTransferObjects;

namespace Property_and_Management.src.Interface
{
    public interface IRentalService
    {
        /// <summary>
        /// Returns all rentals where the user is the renter.
        /// </summary>
        ImmutableList<RentalDataTransferObject> GetRentalsForRenter(int renterId);

        /// <summary>
        /// Returns all rentals where the user is the owner.
        /// </summary>
        ImmutableList<RentalDataTransferObject> GetRentalsForOwner(int ownerId);

        /// <summary>
        /// Returns true if the time slot is available for the given game.
        /// </summary>
        bool IsSlotAvailable(int gameId, DateTime newStart, DateTime newEnd);

        /// <summary>
        /// Creates a confirmed rental for the given game, renter, owner and dates.
        /// Throws if the game owner does not match ownerId or if the slot is taken.
        /// </summary>
        void CreateConfirmedRental(int gameId, int renterId, int ownerId, DateTime startDate, DateTime endDate);
    }
}
