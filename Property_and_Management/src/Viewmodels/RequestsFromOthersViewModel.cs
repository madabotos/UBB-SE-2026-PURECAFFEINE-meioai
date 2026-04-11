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
    public class RequestsFromOthersViewModel : INotifyPropertyChanged, IObserver<RequestDataTransferObject>
    {
        private const int DefaultPageSize = 3;
        private const int FirstPageNumber = 1;
        private const int PageStep = 1;
        private const int NoItemsCount = 0;
        private const int MinimumSuccessfulOperationResult = 1;

        private readonly IRequestService _requestService;
        private readonly ICurrentUserContext _currentUserContext;
        private ObservableCollection<RequestDataTransferObject> _requests = new();
        private ObservableCollection<RequestDataTransferObject> _pagedRequests = new();
        private ImmutableList<RequestDataTransferObject> _allRequests = ImmutableList<RequestDataTransferObject>.Empty;

        public int OwnerId { get; private set; }

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

        public int TotalCount => _allRequests?.Count ?? NoItemsCount;
        public int PageCount => Math.Max(FirstPageNumber, (int)Math.Ceiling((double)TotalCount / PageSize));
        public int DisplayedCount => _pagedRequests?.Count ?? NoItemsCount;

        public ObservableCollection<RequestDataTransferObject> Requests
        {
            get => _requests;
            set
            {
                if (_requests != value)
                {
                    _requests = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<RequestDataTransferObject> PagedRequests
        {
            get => _pagedRequests;
            set
            {
                if (_pagedRequests != value)
                {
                    _pagedRequests = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayedCount));
                    OnPropertyChanged(nameof(TotalCount));
                    OnPropertyChanged(nameof(PageCount));
                    OnPropertyChanged(nameof(ShowingText));
                }
            }
        }

        public string ShowingText => $"Showing {DisplayedCount} of {TotalCount} requests";

        public RequestsFromOthersViewModel(IRequestService requestService, ICurrentUserContext currentUserContext)
        {
            _requestService = requestService;
            _currentUserContext = currentUserContext;
            OwnerId = _currentUserContext.CurrentUserId;
            LoadRequests(FirstPageNumber, PageSize);
        }

        public void LoadRequests(int page, int pageSize)
        {
            OwnerId = _currentUserContext.CurrentUserId;
            var allRequests = _requestService.GetRequestsForOwner(OwnerId)
                .OrderByDescending(request => request.StartDate)
                .ToImmutableList();

            _allRequests = allRequests;
            Requests = new ObservableCollection<RequestDataTransferObject>(allRequests);

            CurrentPage = page;
            UpdatePaging();
        }

        private void UpdatePaging()
        {
            var skip = (CurrentPage - FirstPageNumber) * PageSize;
            var pageItems = _allRequests.Skip(skip).Take(PageSize).ToList();
            PagedRequests = new ObservableCollection<RequestDataTransferObject>(pageItems);
        }

        public void NextPage() => CurrentPage = Math.Min(CurrentPage + PageStep, PageCount);
        public void PrevPage() => CurrentPage = Math.Max(CurrentPage - FirstPageNumber, FirstPageNumber);

        public void ApproveRequest(int requestId)
        {
            var result = _requestService.ApproveRequest(requestId, OwnerId);
            if (result >= MinimumSuccessfulOperationResult) LoadRequests(CurrentPage, PageSize);
        }

        public int DenyRequest(int requestId, string reason)
        {
            var result = _requestService.DenyRequest(requestId, OwnerId, reason);
            if (result >= MinimumSuccessfulOperationResult) LoadRequests(CurrentPage, PageSize);
            return result;
        }

        public int OfferGame(int requestId)
        {
            var result = _requestService.OfferGame(requestId, OwnerId);
            if (result >= MinimumSuccessfulOperationResult) LoadRequests(CurrentPage, PageSize);
            return result;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void OnCompleted() => LoadRequests(CurrentPage, PageSize);
        public void OnError(Exception error) => System.Diagnostics.Debug.WriteLine(error.Message);
        public void OnNext(RequestDataTransferObject value) => LoadRequests(CurrentPage, PageSize);
    }
}
