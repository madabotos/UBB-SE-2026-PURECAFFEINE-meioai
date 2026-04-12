using System.Collections.Immutable;
using System.Linq;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;

namespace Property_and_Management.Src.Viewmodels
{
    public class RentalsToOthersViewModel : PagedViewModel<RentalDataTransferObject>
    {
        private readonly IRentalService rentalService;
        private readonly ICurrentUserContext currentUserContext;

        public int OwnerIdentifier { get; private set; }

        public RentalsToOthersViewModel(IRentalService rentalService, ICurrentUserContext currentUserContext)
        {
            this.rentalService = rentalService;
            this.currentUserContext = currentUserContext;
            Reload();
        }

        public override string ShowingText => $"Showing {DisplayedCount} of {TotalCount} rentals";

        public void LoadRentals() => Reload();

        protected override void Reload()
        {
            OwnerIdentifier = currentUserContext.CurrentUserIdentifier;
            var allRentals = rentalService
                .GetRentalsForOwner(OwnerIdentifier)
                .OrderByDescending(rental => rental.StartDate)
                .ToImmutableList();
            SetAllItems(allRentals);
        }
    }
}
