using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;
using ServerCommunication;

namespace Property_and_Management.src.Viewmodels
{
    public class RequestsToOthersViewModel : INotifyPropertyChanged, IObserver<RequestDTO>
    {
        private readonly IRequestService _requestService;
        private ObservableCollection<RequestDTO> _requests = new();
        private ObservableCollection<RequestDTO> _pagedRequests = new();
        private ImmutableList<RequestDTO> _allRequests = ImmutableList<RequestDTO>.Empty;

        public int RenterId { get; private set; } = (App.Current as App).CurrentUserID;

        private const int s_pageSizeConst = 5;
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
        public int DisplayedCount => PagedRequests?.Count ?? 0;

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

        public RequestsToOthersViewModel(IRequestService requestService)
        {
            _requestService = requestService;
            LoadRequests(1, PageSize);
        }

        // [UI-MRQ-01]
        public void LoadRequests(int page, int pageSize)
        {
            RenterId = (App.Current as App).CurrentUserID;
            _allRequests = _requestService.GetRequestsForRenter(RenterId)  // [UI-MRQ-01]
                .OrderByDescending(r => r.StartDate)  // [UI-MRQ-03]
                .ToImmutableList();

            Requests = new ObservableCollection<RequestDTO>(_allRequests);
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

        //[UI-MRQ-04]
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
