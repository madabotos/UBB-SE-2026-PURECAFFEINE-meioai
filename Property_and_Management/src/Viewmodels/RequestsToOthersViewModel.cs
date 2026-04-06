using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Service;

namespace Property_and_Management.src.Viewmodels
{
    public class RequestsToOthersViewModel : INotifyPropertyChanged, IObserver<RequestDTO>
    {
        private readonly IRequestService _requestService;
        private ObservableCollection<RequestDTO> _requests = new();
        private ObservableCollection<RequestDTO> _pagedRequests = new();
        private ImmutableList<RequestDTO> _allRequests = ImmutableList<RequestDTO>.Empty;

        public int RenterId { get; private set; } = (App.Current as App)?.CurrentUserID ?? 1;

        private const int s_pageSizeConst = 3;
        public static int PageSize => s_pageSizeConst;

        private int _currentPage = 1;
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

        public int TotalCount => _allRequests?.Count ?? 0;
        public int PageCount => Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
        public int DisplayedCount => _pagedRequests?.Count ?? 0;

        public ObservableCollection<RequestDTO> Requests
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

        public ObservableCollection<RequestDTO> PagedRequests
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

        // [REQ-REQ-05] As a Renter, I must be able to see all the games I requested.
        public RequestsToOthersViewModel(IRequestService requestService)
        {
            _requestService = requestService;
            LoadRequests(1, PageSize);
        }

        // [REQ-REQ-07] As a Renter, I should see the requests in descending order by the start date.
        public void LoadRequests(int page, int pageSize)
        {
            RenterId = (App.Current as App)?.CurrentUserID ?? 1;
            var allRequests = _requestService.GetRequestsForRenter(RenterId)
                .OrderByDescending(r => r.StartDate)
                .ToImmutableList();

            _allRequests = allRequests;
            Requests = new ObservableCollection<RequestDTO>(allRequests);

            CurrentPage = page;
            UpdatePaging();
        }

        private void UpdatePaging()
        {
            var skip = (CurrentPage - 1) * PageSize;
            var pageItems = _allRequests.Skip(skip).Take(PageSize).ToList();
            PagedRequests = new ObservableCollection<RequestDTO>(pageItems);
        }

        public void NextPage() => CurrentPage = Math.Min(CurrentPage + 1, PageCount);
        public void PrevPage() => CurrentPage = Math.Max(CurrentPage - 1, 1);

        // [REQ-REQ-06] As a Renter, I must be able to cancel my own pending request at any time.
        public void CancelRequest(int requestId)
        {
            _requestService.CancelRequest(requestId);
            LoadRequests(CurrentPage, PageSize);
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
