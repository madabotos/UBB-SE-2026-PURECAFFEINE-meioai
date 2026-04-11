using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;

namespace Property_and_Management.src.Viewmodels
{
    public class RentalsFromOthersViewModel : INotifyPropertyChanged
    {
        private const int DefaultPageSize = 3;
        private const int FirstPageNumber = 1;
        private const int PageStep = 1;
        private const int NoItemsCount = 0;

        private readonly IRentalService _rentalService;
        private readonly ICurrentUserContext _currentUserContext;
        private ObservableCollection<RentalDTO> _rentals = new();
        private ObservableCollection<RentalDTO> _pagedRentals = new();
        private ImmutableList<RentalDTO> _allRentals = ImmutableList<RentalDTO>.Empty;

        public int RenterId { get; private set; }

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

        public ObservableCollection<RentalDTO> Rentals
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

        public ObservableCollection<RentalDTO> PagedRentals
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

        public RentalsFromOthersViewModel(IRentalService rentalService, ICurrentUserContext currentUserContext)
        {
            _rentalService = rentalService;
            _currentUserContext = currentUserContext;
            RenterId = _currentUserContext.CurrentUserId;
            LoadRentals(FirstPageNumber, PageSize);
        }

        public void LoadRentals(int page, int pageSize)
        {
            RenterId = _currentUserContext.CurrentUserId;
            var allRentals = _rentalService.GetRentalsForRenter(RenterId)
                .OrderByDescending(rental => rental.StartDate)
                .ToImmutableList();

            _allRentals = allRentals;
            Rentals = new ObservableCollection<RentalDTO>(allRentals);

            CurrentPage = page;
            UpdatePaging();
        }

        private void UpdatePaging()
        {
            var skip = (CurrentPage - FirstPageNumber) * PageSize;
            var pageItems = _allRentals.Skip(skip).Take(PageSize).ToList();
            PagedRentals = new ObservableCollection<RentalDTO>(pageItems);
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
