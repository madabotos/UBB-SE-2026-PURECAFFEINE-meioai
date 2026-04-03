using System.Collections.Immutable;
using Microsoft.Data.SqlClient;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Interface
{
    public interface IRentalRepository : IRepository<Rental>
    {
        /// <summary>
        /// Gets rentals for which the specified user is the owner.
        /// </summary>
        ImmutableList<Rental> GetRentalsByOwner(int ownerId);

        /// <summary>
        /// Gets rentals created by the specified renter.
        /// </summary>
        ImmutableList<Rental> GetRentalsByRenter(int renterId);

        /// <summary>
        /// Gets rentals for the specified game.
        /// </summary>
        ImmutableList<Rental> GetRentalsByGame(int gameId);

        /// <summary>
        /// Inserts the rental using an existing connection and transaction.
        /// </summary>
        void Add(Rental entity, SqlConnection connection, SqlTransaction transaction);
    }
}
