using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.UI.Dispatching;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;

namespace Property_and_Management.Src.Viewmodels
{
    public class NotificationsViewModel : PagedViewModel<NotificationDataTransferObject>,
                                           IObserver<NotificationDataTransferObject>,
                                           IDisposable
    {
        private const int InvalidUserIdentifier = 0;
        private const int DefaultUserIdentifier = 1;

        private readonly INotificationService notificationService;
        private readonly IRequestService requestService;
        private readonly ICurrentUserContext currentUserContext;
        private readonly IDismissedNotificationStore dismissedNotificationStore;
        private readonly IDisposable subscription;

        // Captured at construction (on the UI thread via DI) so we can marshal
        // OnNext calls back to the UI thread. Incoming notifications arrive on a
        // thread-pool thread from the UDP listener and WinUI ObservableCollection
        // writes from a non-UI thread either throw or silently drop updates.
        private readonly DispatcherQueue? dispatcherQueue;

        private HashSet<int> dismissedNotificationIds = new HashSet<int>();

        public int CurrentUserIdentifier { get; private set; }

        public NotificationsViewModel(
            INotificationService notificationService,
            IRequestService requestService,
            ICurrentUserContext currentUserContext,
            IDismissedNotificationStore dismissedNotificationStore)
        {
            this.notificationService = notificationService;
            this.requestService = requestService;
            this.currentUserContext = currentUserContext;
            this.dismissedNotificationStore = dismissedNotificationStore;

            // DI resolution happens from App.InitializeServices on the UI thread,
            // so GetForCurrentThread() returns the UI dispatcher. If ever
            // constructed off the UI thread this will be null and OnNext will
            // fall back to in-place.
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            LoadNotificationsForUser(currentUserContext.CurrentUserIdentifier);

            subscription = notificationService.Subscribe(this);
        }

        public void LoadNotificationsForUser(int userIdentifier)
        {
            CurrentUserIdentifier = userIdentifier;
            dismissedNotificationIds = dismissedNotificationStore.Load(userIdentifier) ?? new HashSet<int>();
            Reload();
        }

        protected override void Reload()
        {
            var notifications = notificationService
                .GetNotificationsForUser(CurrentUserIdentifier)
                .Where(notification => !dismissedNotificationIds.Contains(notification.Identifier))
                .OrderByDescending(notification => notification.Identifier)
                .ToImmutableList();

            SetAllItems(notifications);
        }

        public void DeleteNotificationByIdentifier(int notificationIdentifier)
        {
            // REQ-NOT-02: dismiss locally from view and persist locally, keep
            // DB history untouched.
            dismissedNotificationIds.Add(notificationIdentifier);
            dismissedNotificationStore.Save(CurrentUserIdentifier, dismissedNotificationIds);
            Reload();
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
            System.Diagnostics.Debug.WriteLine($"Notification observable error: {error.Message}");
        }

        public void OnNext(NotificationDataTransferObject value)
        {
            // Incoming notifications are published from the UDP listener's
            // thread-pool thread. All downstream work updates ObservableCollection
            // bindings and must run on the UI thread.
            var targetUserIdentifier = CurrentUserIdentifier == InvalidUserIdentifier
                ? DefaultUserIdentifier
                : CurrentUserIdentifier;

            if (dispatcherQueue != null && !dispatcherQueue.HasThreadAccess)
            {
                dispatcherQueue.TryEnqueue(() => LoadNotificationsForUser(targetUserIdentifier));
                return;
            }

            LoadNotificationsForUser(targetUserIdentifier);
        }

        /// <summary>
        /// Approve a pending offer on behalf of the current user. Returns a
        /// user-friendly error message or null on success. Keeps error-code
        /// mapping in the view model so the notifications page code-behind
        /// stays free of service-namespace types.
        /// </summary>
        public string? TryApproveOffer(int requestIdentifier)
        {
            var result = requestService.ApproveOffer(requestIdentifier, CurrentUserIdentifier);
            if (result.IsSuccess)
            {
                LoadNotificationsForUser(CurrentUserIdentifier);
                return null;
            }

            return result.Error switch
            {
                ApproveOfferError.NotFound => "Request not found.",
                ApproveOfferError.NotRenter => "You are not the renter for this request.",
                ApproveOfferError.NoPendingOffer => "This offer is no longer pending.",
                ApproveOfferError.TransactionFailed => "Could not approve the offer. Please try again.",
                _ => Constants.DialogMessages.UnexpectedErrorOccurred
            };
        }

        /// <summary>
        /// Deny a pending offer on behalf of the current user. Returns a
        /// user-friendly error message or null on success.
        /// </summary>
        public string? TryDenyOffer(int requestIdentifier)
        {
            var result = requestService.DenyOffer(requestIdentifier, CurrentUserIdentifier);
            if (result.IsSuccess)
            {
                LoadNotificationsForUser(CurrentUserIdentifier);
                return null;
            }

            return result.Error switch
            {
                DenyOfferError.NotFound => "Request not found.",
                DenyOfferError.NotRenter => "You are not the renter for this request.",
                DenyOfferError.NoPendingOffer => "This offer is no longer pending.",
                _ => Constants.DialogMessages.UnexpectedErrorOccurred
            };
        }

        public void Dispose() => subscription?.Dispose();
    }
}
