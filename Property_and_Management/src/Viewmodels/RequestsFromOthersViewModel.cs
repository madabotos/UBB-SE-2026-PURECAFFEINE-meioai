using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Service;
using Property_and_Management.src.Interface;
using ServerCommunication;

namespace Property_and_Management.src.Viewmodels
{
    public class RequestsFromOthersViewModel : INotifyPropertyChanged, IObserver<RequestDTO>
    {
        private readonly IRequestService _requestService;
        private ObservableCollection<RequestDTO> _requests = new();
        private ObservableCollection<RequestDTO> _pagedRequests = new();
        private ImmutableList<RequestDTO> _allRequests = ImmutableList<RequestDTO>.Empty;

        public int ownerId { get; private set; }  // Matches UML exactly

        private const int pageSizeConst = 5;
        public static int PageSize => pageSizeConst;

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    OnPropertyChanged();  // Fixed typo
                    UpdatePaging();
                }
            }
        }

        public int TotalCount => _allRequests?.Count ?? 0;
        public int PageCount => Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
        public int DisplayedCount => _pagedRequests?.Count ?? 0;

        public ObservableCollection<RequestDTO> requests
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

        public ObservableCollection<RequestDTO> pagedRequests
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

        public RequestsFromOthersViewModel(IRequestService requestService)
        {
            _requestService = requestService;
            LoadRequests(1, PageSize);  // Matches UML method signature exactly
        }

        // EXACTLY matches UML signature [UI-ORQ-01]
        public void LoadRequests(int page, int pageSize)
        {
            ownerId = 1; // Get from auth service in real app
            var allRequests = _requestService.GetRequestsForOwner(ownerId)
                .OrderByDescending(r => r.StartDate)  // [UI-ORQ-03]
                .ToImmutableList();

            _allRequests = allRequests;
            requests = new ObservableCollection<RequestDTO>(allRequests);

            CurrentPage = page;
            UpdatePaging();
        }

        private void UpdatePaging()
        {
            var skip = (CurrentPage - 1) * PageSize;
            var pageItems = _allRequests.Skip(skip).Take(PageSize).ToList();
            pagedRequests = new ObservableCollection<RequestDTO>(pageItems);
        }

        public void NextPage() => CurrentPage = Math.Min(CurrentPage + 1, PageCount);
        public void PrevPage() => CurrentPage = Math.Max(CurrentPage - 1, 1);

        public void ApproveRequest(int requestId)
        {
            var result = _requestService.ApproveRequest(requestId, ownerId);
            if (result > 0) LoadRequests(CurrentPage, PageSize);
        }

        public void DenyRequest(int requestId, string reason)
        {
            var result = _requestService.DenyRequest(requestId, ownerId, reason);
            if (result > 0) LoadRequests(CurrentPage, PageSize);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void OnCompleted() => LoadRequests(CurrentPage, PageSize);
        public void OnError(Exception error) => System.Diagnostics.Debug.WriteLine(error.Message);
        public void OnNext(RequestDTO value) => LoadRequests(CurrentPage, PageSize);
    }
}
