using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.UI.Dispatching;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;

namespace Property_and_Management.Src.Viewmodels
{
    public class NotificationsViewModel : PagedViewModel<NotificationDTO>,
                                           IObserver<NotificationDTO>,
                                           IDisposable
    {
        private const int InvalidUserId = 0;
        private const int DefaultUserId = 1;

        private readonly INotificationService notificationService;
        private readonly IDisposable subscription;

        private readonly DispatcherQueue? dispatcherQueue;

        public int currentUserId { get; private set; }

        public NotificationsViewModel(
            INotificationService notificationService,
            ICurrentUserContext currentUserContext)
        {
            this.notificationService = notificationService;

            dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            LoadNotificationsForUser(currentUserContext.currentUserId);

            subscription = notificationService.Subscribe(this);
        }

        public void LoadNotificationsForUser(int userId)
        {
            currentUserId = userId;
            Reload();
        }

        protected override void Reload()
        {
            var notifications = notificationService
                .GetNotificationsForUser(currentUserId)
                .OrderByDescending(notification => notification.Id)
                .ToImmutableList();

            SetAllItems(notifications);
        }

        public void DeleteNotificationByIdentifier(int notificationId)
        {
            try
            {
                notificationService.DeleteNotificationByIdentifier(notificationId);
            }
            catch (KeyNotFoundException)
            {
            }

            Reload();
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
            System.Diagnostics.Debug.WriteLine($"Notification observable error: {error.Message}");
        }

        public void OnNext(NotificationDTO value)
        {
            var targetUserId = currentUserId == InvalidUserId
                ? DefaultUserId
                : currentUserId;

            if (dispatcherQueue != null && !dispatcherQueue.HasThreadAccess)
            {
                dispatcherQueue.TryEnqueue(() => LoadNotificationsForUser(targetUserId));
                return;
            }

            LoadNotificationsForUser(targetUserId);
        }

        public void Dispose() => subscription?.Dispose();
    }
}