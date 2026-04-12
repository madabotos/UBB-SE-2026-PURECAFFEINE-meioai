using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Property_and_Management.Src.Viewmodels
{
    /// <summary>
    /// Base class for paged list ViewModels. Owns the pagination state
    /// (<see cref="CurrentPage"/>, <see cref="PagedItems"/>, <see cref="ShowingText"/>)
    /// so every page-based VM does not have to re-implement clamping, paging, and
    /// property notifications.
    ///
    /// Subclasses override <see cref="Reload"/> to rebuild their result set from
    /// their backing service, and call <see cref="SetAllItems"/> to publish the
    /// new data to the bound <see cref="PagedItems"/> collection.
    /// </summary>
    public abstract class PagedViewModel<T> : INotifyPropertyChanged
    {
        protected const int DefaultPageSize = 3;
        protected const int FirstPageNumber = 1;
        protected const int PageStep = 1;
        private const int NoItemsCount = 0;

        private ImmutableList<T> allItems = ImmutableList<T>.Empty;
        private ObservableCollection<T> pagedItems = new ObservableCollection<T>();
        private int currentPage = FirstPageNumber;

        /// <summary>
        /// Read-only view of the complete (unpaged) result set. Subclasses assign via
        /// <see cref="SetAllItems"/>.
        /// </summary>
        protected ImmutableList<T> AllItems => allItems;

        /// <summary>Items displayed on the current page. Bound by views.</summary>
        public ObservableCollection<T> PagedItems
        {
            get => pagedItems;
            private set
            {
                if (pagedItems != value)
                {
                    pagedItems = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayedCount));
                    OnPropertyChanged(nameof(TotalCount));
                    OnPropertyChanged(nameof(PageCount));
                    OnPropertyChanged(nameof(CurrentPage));
                    OnPropertyChanged(nameof(ShowingText));
                }
            }
        }

        /// <summary>Current page index, 1-based. Setter clamps to [FirstPageNumber..PageCount].</summary>
        public int CurrentPage
        {
            get => currentPage;
            set
            {
                var clamped = Math.Max(FirstPageNumber, Math.Min(value, PageCount));
                if (currentPage != clamped)
                {
                    currentPage = clamped;
                    OnPropertyChanged();
                    UpdatePaging();
                }
            }
        }

        public static int PageSize => DefaultPageSize;

        public int TotalCount => allItems?.Count ?? NoItemsCount;

        public int PageCount => Math.Max(
            FirstPageNumber,
            (int)Math.Ceiling((double)TotalCount / PageSize));

        public int DisplayedCount => pagedItems?.Count ?? NoItemsCount;

        /// <summary>
        /// "Showing X of Y" text for the footer. Virtual so subclasses can add
        /// domain vocabulary ("requests", "rentals", "games").
        /// </summary>
        public virtual string ShowingText => $"Showing {DisplayedCount} of {TotalCount}";

        public virtual void NextPage()
        {
            if (CurrentPage < PageCount)
            {
                CurrentPage += PageStep;
            }
        }

        public virtual void PrevPage()
        {
            if (CurrentPage > FirstPageNumber)
            {
                CurrentPage -= PageStep;
            }
        }

        /// <summary>
        /// Subclasses rebuild the underlying result set by calling their service,
        /// then publish it via <see cref="SetAllItems"/>.
        /// </summary>
        protected abstract void Reload();

        /// <summary>
        /// Assign the complete result set and recompute the current page. Keeps
        /// the current page number where possible, clamping if the new list is shorter.
        /// </summary>
        protected void SetAllItems(ImmutableList<T> items)
        {
            allItems = items ?? ImmutableList<T>.Empty;
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(PageCount));

            if (currentPage > PageCount)
            {
                currentPage = PageCount;
                OnPropertyChanged(nameof(CurrentPage));
            }

            UpdatePaging();
        }

        /// <summary>
        /// Re-run <see cref="Reload"/> on demand (used by UI refresh handlers).
        /// </summary>
        public void Refresh() => Reload();

        private void UpdatePaging()
        {
            var skip = (CurrentPage - FirstPageNumber) * PageSize;
            var pageItems = allItems.Skip(skip).Take(PageSize).ToList();
            PagedItems = new ObservableCollection<T>(pageItems);

            OnPropertyChanged(nameof(DisplayedCount));
            OnPropertyChanged(nameof(ShowingText));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
