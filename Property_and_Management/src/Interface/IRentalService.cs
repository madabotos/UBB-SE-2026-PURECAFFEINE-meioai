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
        ImmutableList<RentalDataTransferObject> GetRentalsForRenter(int renterIdentifier);

        /// <summary>
        /// Returns all rentals where the user is the owner.
        /// </summary>
        ImmutableList<RentalDataTransferObject> GetRentalsForOwner(int ownerIdentifier);

        /// <summary>
        /// Returns true if the time slot is available for the given game.
        /// </summary>
        bool IsSlotAvailable(int gameIdentifier, DateTime newStart, DateTime newEnd);

        /// <summary>
        /// Creates a confirmed rental for the given game, renter, owner and dates.
        /// Throws if the game owner does not match ownerIdentifier or if the slot is taken.
        /// </summary>
        void CreateConfirmedRental(int gameIdentifier, int renterIdentifier, int ownerIdentifier, DateTime startDate, DateTime endDate);
    }
}

