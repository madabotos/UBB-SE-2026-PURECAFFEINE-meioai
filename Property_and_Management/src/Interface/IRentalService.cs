using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Interface
{
    public interface IRentalService
    {
        /// <summary>
        /// Returns ImmutableList<RentalDTO> of all rentals where the user is the renter.
        /// </summary>
        /// <param name="renterId"></param>
        /// <returns></returns>
        ImmutableList<RentalDTO> GetRentalsForRenter(int renterId);

        /// <summary>
        /// Returns ImmutableList<RentalDTO> of all rentals where the user is the owner.
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        ImmutableList<RentalDTO> GetRentalsForOwner(int ownerId);

        /// <summary>
        /// Returns bool indicating whether the time slot is available for the given game.
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="newStart"></param>
        /// <param name="newEnd"></param>
        /// <returns></returns>
        bool IsSlotAvailable(int gameId, DateTime newStart, DateTime newEnd);

        /// <summary>
        /// Creates a rental
        /// </summary>
        /// <param name="rental"></param>
        public void CreateConfirmedRental(Rental rental);
    }
}
