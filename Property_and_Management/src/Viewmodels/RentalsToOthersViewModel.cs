using System.Collections.Immutable;
using System.Linq;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;

namespace Property_and_Management.Src.Viewmodels
{
    public class RentalsToOthersViewModel : PagedViewModel<RentalDTO>
    {
        private readonly IRentalService rentalService;
        private readonly ICurrentUserContext currentUserContext;

        public int ownerId { get; private set; }

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
            ownerId = currentUserContext.currentUserId;
            var allRentals = rentalService
                .GetRentalsForOwner(ownerId)
                .OrderByDescending(rental => rental.StartDate)
                .ToImmutableList();
            SetAllItems(allRentals);
        }
    }
}