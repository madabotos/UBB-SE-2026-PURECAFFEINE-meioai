using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls.Primitives;
using Property_and_Management.src.DataTransferObjects;
using Property_and_Management.src.Interface;
using Windows.ApplicationModel.VoiceCommands;

namespace Property_and_Management.src.Viewmodels
{
    public class NotificationsViewModel : INotifyPropertyChanged, IObserver<NotificationDataTransferObject>, IDisposable
    {
        private const int DefaultPageSize = 3;
        private const int FirstPageNumber = 1;
        private const int PageStep = 1;
        private const int InvalidNotificationId = -1;
        private const int MinimumValidNotificationId = 0;
        private const int NoItemsCount = 0;
        private const int MinimumSuccessfulOperationResult = 1;
        private const int DefaultUserIdentifier = 1;

        private ObservableCollection<NotificationDataTransferObject> _notifications = new ObservableCollection<NotificationDataTransferObject>();
        private ObservableCollection<NotificationDataTransferObject> _pagedNotifications = new ObservableCollection<NotificationDataTransferObject>();
        private readonly INotificationService _notificationService;
        private readonly IRequestService _requestService;
        private readonly ICurrentUserContext _currentUserContext;
        private readonly IDisposable _subscription;
        private HashSet<int> _dismissedNotificationIds = new HashSet<int>();

        private ImmutableList<NotificationDataTransferObject> _allNotifications = ImmutableList<NotificationDataTransferObject>.Empty;

        public int CurrentUserIdentifier { get; private set; }

        public static int PageSize => DefaultPageSize;

        private int _currentPage = FirstPageNumber;
        public int CurrentPage
        {
            get => _currentPage;
            private set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    OnPropertyChanged();
                    UpdatePaging();
                }
            }
        }

        public int TotalCount => _allNotifications?.Count ?? NoItemsCount;

        public int PageCount => Math.Max(FirstPageNumber, (int)Math.Ceiling((double)TotalCount / PageSize));

        public int DisplayedCount => PagedNotifications?.Count ?? NoItemsCount;

        public ObservableCollection<NotificationDataTransferObject> Notifications
        {
            get => _notifications;
            set
            {
                if (_notifications != value)
                {
                    _notifications = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<NotificationDataTransferObject> PagedNotifications
        {
            get => _pagedNotifications;
            private set
            {
                if (_pagedNotifications != value)
                {
                    _pagedNotifications = value;
                    OnPropertyChanged(nameof(PagedNotifications));
                    OnPropertyChanged(nameof(DisplayedCount));
                    OnPropertyChanged(nameof(TotalCount));
                    OnPropertyChanged(nameof(PageCount));
                    OnPropertyChanged(nameof(CurrentPage));
                    OnPropertyChanged(nameof(ShowingText));
                }
            }
        }

        public string ShowingText => $"Showing {DisplayedCount} of {TotalCount}";

        public NotificationsViewModel(INotificationService notificationService, IRequestService requestService,
                                      ICurrentUserContext currentUserContext)
        {
            _notificationService = notificationService;
            _requestService = requestService;
            _currentUserContext = currentUserContext;

            LoadNotificationsForUser(_currentUserContext.CurrentUserIdentifier);

            _subscription = notificationService.Subscribe(this);
        }

        public void LoadNotificationsForUser(int userIdentifier)
        {
            CurrentUserIdentifier = userIdentifier;
            LoadDismissedIdsForCurrentUser();

            _allNotifications = _notificationService
                .GetNotificationsForUser(userIdentifier)
                .Where(notification => !_dismissedNotificationIds.Contains(notification.Identifier))
                .OrderByDescending(notification => notification.Identifier)
                .ToImmutableList();

            Notifications = new ObservableCollection<NotificationDataTransferObject>(_allNotifications);

            // reset paging
            _currentPage = FirstPageNumber;
            UpdatePaging();

            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(PageCount));
            OnPropertyChanged(nameof(ShowingText));
        }

        // small internal wrapper to convert repository results -> Data Transfer Object list (keeps call sites short)
        private System.Collections.Immutable.ImmutableList<NotificationDataTransferObject> GetNotificationsForCurrentUser()
        {
            return _notificationService.GetNotificationsForUser(CurrentUserIdentifier);
        }

        private void UpdatePaging()
        {
            if (_allNotifications == null) _allNotifications = ImmutableList<NotificationDataTransferObject>.Empty;

            if (CurrentPage < FirstPageNumber) _currentPage = FirstPageNumber;
            if (CurrentPage > PageCount) _currentPage = PageCount;

            var skip = (CurrentPage - FirstPageNumber) * PageSize;
            var pageItems = _allNotifications.Skip(skip).Take(PageSize).ToList();
            PagedNotifications = new ObservableCollection<NotificationDataTransferObject>(pageItems);

            OnPropertyChanged(nameof(DisplayedCount));
            OnPropertyChanged(nameof(ShowingText));
        }

        public void NextPage()
        {
            if (CurrentPage < PageCount)
            {
                CurrentPage += PageStep;
            }
        }

        public void PrevPage()
        {
            if (CurrentPage > FirstPageNumber)
            {
                CurrentPage -= PageStep;
            }
        }

        public void DeleteNotificationByIdentifier(int notificationIdentifier)
        {
            // REQ-NOT-02: dismiss locally from view and persist locally, keep DB history untouched
            _dismissedNotificationIds.Add(notificationIdentifier);
            SaveDismissedIdsForCurrentUser();

            _allNotifications = GetNotificationsForCurrentUser()
                .Where(notification => !_dismissedNotificationIds.Contains(notification.Identifier))
                .OrderByDescending(notification => notification.Identifier)
                .ToImmutableList();
            Notifications = new ObservableCollection<NotificationDataTransferObject>(_allNotifications);

            // ensure current page is valid
            if (CurrentPage > PageCount) _currentPage = PageCount;
            UpdatePaging();

            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(PageCount));
            OnPropertyChanged(nameof(ShowingText));
        }

        private string GetDismissedStoragePath()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BoardRent");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, $"dismissed-notifications-user-{CurrentUserIdentifier}.txt");
        }

        private void LoadDismissedIdsForCurrentUser()
        {
            var path = GetDismissedStoragePath();
            if (!File.Exists(path))
            {
                _dismissedNotificationIds = new HashSet<int>();
                return;
            }

            var serialized = File.ReadAllText(path);

            if (string.IsNullOrWhiteSpace(serialized))
            {
                _dismissedNotificationIds = new HashSet<int>();
                return;
            }

            _dismissedNotificationIds = serialized
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(token => int.TryParse(token, out var id) ? id : InvalidNotificationId)
                .Where(id => id > MinimumValidNotificationId)
                .ToHashSet();
        }

        private void SaveDismissedIdsForCurrentUser()
        {
            var path = GetDismissedStoragePath();
            File.WriteAllText(path, string.Join(",", _dismissedNotificationIds.OrderBy(id => id)));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void OnCompleted()
        {
            Console.WriteLine("Notification observable completed");
        }

        public void OnError(Exception error)
        {
            Console.WriteLine($"Notification observable error: {error.Message}");
        }

        public void OnNext(NotificationDataTransferObject value)
        {
            // Trigger an update from the service
            LoadNotificationsForUser(CurrentUserIdentifier == MinimumValidNotificationId ? DefaultUserIdentifier : CurrentUserIdentifier);
        }

        public int ApproveOffer(int requestIdentifier)
        {
            var result = _requestService.ApproveOffer(requestIdentifier, CurrentUserIdentifier);
            if (result >= MinimumSuccessfulOperationResult) LoadNotificationsForUser(CurrentUserIdentifier);
            return result;
        }

        public int DenyOffer(int requestIdentifier)
        {
            var result = _requestService.DenyOffer(requestIdentifier, CurrentUserIdentifier);
            if (result >= MinimumSuccessfulOperationResult) LoadNotificationsForUser(CurrentUserIdentifier);
            return result;
        }

        public void Dispose() => _subscription?.Dispose();
    }
}



