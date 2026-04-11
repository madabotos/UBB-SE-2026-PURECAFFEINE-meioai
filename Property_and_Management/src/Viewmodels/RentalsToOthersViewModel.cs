using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Property_and_Management.src.DataTransferObjects;
using Property_and_Management.src.Interface;

namespace Property_and_Management.src.Viewmodels
{
    public class RentalsToOthersViewModel : INotifyPropertyChanged
    {
        private const int DefaultPageSize = 3;
        private const int FirstPageNumber = 1;
        private const int PageStep = 1;
        private const int NoItemsCount = 0;

        private readonly IRentalService _rentalService;
        private readonly ICurrentUserContext _currentUserContext;
        private ObservableCollection<RentalDataTransferObject> _rentals = new();
        private ObservableCollection<RentalDataTransferObject> _pagedRentals = new();
        private ImmutableList<RentalDataTransferObject> _allRentals = ImmutableList<RentalDataTransferObject>.Empty;

        public int ownerIdentifier { get; private set; }

        public static int PageSize => DefaultPageSize;

        private int _currentPage = FirstPageNumber;
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    OnPropertyChanged();
                    UpdatePaging();
                }
            }
        }

        public int TotalCount => _allRentals?.Count ?? NoItemsCount;
        public int PageCount => Math.Max(FirstPageNumber, (int)Math.Ceiling((double)TotalCount / PageSize));
        public int DisplayedCount => _pagedRentals?.Count ?? NoItemsCount;

        public ObservableCollection<RentalDataTransferObject> Rentals
        {
            get => _rentals;
            set
            {
                if (_rentals != value)
                {
                    _rentals = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<RentalDataTransferObject> PagedRentals
        {
            get => _pagedRentals;
            set
            {
                if (_pagedRentals != value)
                {
                    _pagedRentals = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayedCount));
                    OnPropertyChanged(nameof(TotalCount));
                    OnPropertyChanged(nameof(PageCount));
                    OnPropertyChanged(nameof(ShowingText));
                }
            }
        }

        public string ShowingText => $"Showing {DisplayedCount} of {TotalCount} rentals";

        public RentalsToOthersViewModel(IRentalService rentalService, ICurrentUserContext currentUserContext)
        {
            _rentalService = rentalService;
            _currentUserContext = currentUserContext;
            ownerIdentifier = _currentUserContext.CurrentUserIdentifier;
            LoadRentals(FirstPageNumber, PageSize);
        }

        public void LoadRentals(int page, int pageSize)
        {
            ownerIdentifier = _currentUserContext.CurrentUserIdentifier;
            var allRentals = _rentalService.GetRentalsForOwner(ownerIdentifier)
                .OrderByDescending(rental => rental.StartDate)
                .ToImmutableList();

            _allRentals = allRentals;
            Rentals = new ObservableCollection<RentalDataTransferObject>(allRentals);

            CurrentPage = page;
            UpdatePaging();
        }

        private void UpdatePaging()
        {
            var skip = (CurrentPage - FirstPageNumber) * PageSize;
            var pageItems = _allRentals.Skip(skip).Take(PageSize).ToList();
            PagedRentals = new ObservableCollection<RentalDataTransferObject>(pageItems);
        }

        public void NextPage() => CurrentPage = Math.Min(CurrentPage + PageStep, PageCount);
        public void PrevPage() => CurrentPage = Math.Max(CurrentPage - FirstPageNumber, FirstPageNumber);

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}


