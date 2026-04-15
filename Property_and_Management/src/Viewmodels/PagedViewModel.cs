using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Property_and_Management.Src.Viewmodels
{
    public abstract class PagedViewModel<T> : INotifyPropertyChanged
    {
        protected const int DefaultPageSize = 3;
        protected const int FirstPageNumber = 1;
        protected const int PageStep = 1;
        private const int NoItemsCount = 0;

        private ImmutableList<T> allItems = ImmutableList<T>.Empty;
        private ObservableCollection<T> pagedItems = new ObservableCollection<T>();
        private int currentPage = FirstPageNumber;

        protected ImmutableList<T> AllItems => allItems;

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

        protected abstract void Reload();

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