using System.Collections.Immutable;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Interface
{
    public interface IRentalRepository : IRepository<Rental>
    {
        /// <summary>
        /// Inserts a rental using repository persistence semantics.
        /// Availability/business validation is handled in the service layer.
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

