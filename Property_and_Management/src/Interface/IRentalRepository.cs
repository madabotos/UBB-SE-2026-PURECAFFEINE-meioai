using System.Collections.Immutable;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Interface
{
    public interface IRentalRepository : IRepository<Rental>
    {
        /// <summary>
        /// Inserts a rental inside a serializable transaction after verifying the time slot
        /// is not within the 48-hour buffer of an existing rental.
        /// Throws <see cref="System.InvalidOperationException"/> if the slot is taken.
        /// </summary>
        void AddConfirmed(Rental entity);

        /// <summary>
        /// Gets rentals for which the specified user is the owner.
        /// </summary>
        ImmutableList<Rental> GetRentalsByOwner(int ownerIdentifier);

        /// <summary>
        /// Gets rentals created by the specified renter.
        /// </summary>
        ImmutableList<Rental> GetRentalsByRenter(int renterIdentifier);

        /// <summary>
        /// Gets rentals for the specified game.
        /// </summary>
        ImmutableList<Rental> GetRentalsByGame(int gameIdentifier);
    }
}

