using System.Collections.Immutable;
using System.Linq;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;

namespace Property_and_Management.Src.Viewmodels
{
    public class RentalsFromOthersViewModel : PagedViewModel<RentalDataTransferObject>
    {
        private readonly IRentalService rentalService;
        private readonly ICurrentUserContext currentUserContext;

        public int RenterIdentifier { get; private set; }

        public RentalsFromOthersViewModel(IRentalService rentalService, ICurrentUserContext currentUserContext)
        {
            this.rentalService = rentalService;
            this.currentUserContext = currentUserContext;
            Reload();
        }

        public override string ShowingText => $"Showing {DisplayedCount} of {TotalCount} rentals";

        /// <summary>
        /// Public alias retained so navigation can request a full refresh
        /// without reaching into the base class.
        /// </summary>
        public void LoadRentals() => Reload();

        protected override void Reload()
        {
            RenterIdentifier = currentUserContext.CurrentUserIdentifier;
            var allRentals = rentalService
                .GetRentalsForRenter(RenterIdentifier)
                .OrderByDescending(rental => rental.StartDate)
                .ToImmutableList();
            SetAllItems(allRentals);
        }
    }
}
