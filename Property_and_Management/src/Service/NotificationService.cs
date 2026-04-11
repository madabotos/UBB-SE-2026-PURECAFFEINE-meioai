using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Property_and_Management;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Service
{
    public class NotificationService : INotificationService, IObserver<IncomingNotification>, IObservable<NotificationDataTransferObject>, IDisposable
    {
        private const int ReminderLeadDays = 1;
        private const int NewEntityIdentifier = 0;
        private const int MissingUserIdentifier = 0;

        private bool disposed;
        private readonly CancellationTokenSource scheduleCancellationTokenSource = new();
        private readonly INotificationRepository notificationRepository;
        private readonly IMapper<Notification, NotificationDataTransferObject> notificationMapper;
        private readonly IServerClient serverClient;
        private readonly ICurrentUserContext currentUserContext;
        private readonly IToastNotificationService toastNotificationService;

        // subscribers is mutated from the UI thread (Subscribe/Unsubscriber.Dispose)
        // and published to from the UDP listener thread (OnNext → NotifySubscribers).
        // All access goes through subscribersLock so iteration snapshots are stable.
        private readonly List<IObserver<NotificationDataTransferObject>> subscribers = new();
        private readonly object subscribersLock = new();

        public NotificationService(
            INotificationRepository notificationRepository,
            IMapper<Notification, NotificationDataTransferObject> notificationMapper,
            IServerClient serverClient,
            ICurrentUserContext currentUserContext,
            IToastNotificationService toastNotificationService)
        {
            this.notificationRepository = notificationRepository;
            this.notificationMapper = notificationMapper;
            this.serverClient = serverClient;
            this.currentUserContext = currentUserContext;
            this.toastNotificationService = toastNotificationService;
            serverClient.Subscribe(this);
        }

        public NotificationDataTransferObject DeleteNotificationByIdentifier(int notificationIdentifier) =>
            notificationMapper.ToDataTransferObject(notificationRepository.Delete(notificationIdentifier));

        public NotificationDataTransferObject GetNotificationByIdentifier(int notificationIdentifier) =>
            notificationMapper.ToDataTransferObject(notificationRepository.Get(notificationIdentifier));

        public ImmutableList<NotificationDataTransferObject> GetNotificationsForUser(int userIdentifier) =>
            notificationRepository
                .GetNotificationsByUser(userIdentifier)
                .Select(notification => notificationMapper.ToDataTransferObject(notification))
                .ToImmutableList();

        public void SendNotificationToUser(int userIdentifier, NotificationDataTransferObject notification)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            DateTime timestamp = notification.Timestamp == default ? DateTime.UtcNow : notification.Timestamp;

            var notificationModel = BuildNotificationModel(
                userIdentifier,
                timestamp,
                notification.Title,
                notification.Body,
                notification.Type,
                notification.RelatedRequestIdentifier);

            notificationRepository.Add(notificationModel);

            if (currentUserContext.CurrentUserIdentifier == userIdentifier)
            {
                NotifySubscribers(BuildNotificationDataTransferObject(
                    notificationModel.Identifier,
                    userIdentifier,
                    timestamp,
                    notification.Title,
                    notification.Body,
                    notification.Type,
                    notification.RelatedRequestIdentifier));
            }

            serverClient.SendNotification(userIdentifier, notification.Title, notification.Body);
        }

        public void DeleteNotificationsByRequestId(int requestIdentifier)
        {
            notificationRepository.DeleteByRequestId(requestIdentifier);
        }

        public void UpdateNotificationByIdentifier(int notificationIdentifier, NotificationDataTransferObject notification)
        {
            notificationRepository.Update(notificationIdentifier, notificationMapper.ToModel(notification));
        }

        public void StartListening()
        {
            // Fire-and-forget, but observe any failure so it doesn't become a silent
            // unobserved task exception. The listen loop runs for the lifetime of the
            // process, so we only ever expect to get here on a hard fault.
            _ = Task.Run(async () =>
            {
                try
                {
                    await serverClient.ListenAsync();
                }
                catch (Exception listenException)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"NotificationService: listen loop terminated — {listenException}");
                }
            });
        }

        public void StopListening() => serverClient.StopListening();
        public void OnCompleted()
        {
        }
        public void OnError(Exception error)
        {
        }

        public void OnNext(IncomingNotification incomingNotification)
        {
            var notificationDataTransferObject = BuildNotificationDataTransferObject(
                NewEntityIdentifier,
                incomingNotification.UserIdentifier,
                incomingNotification.Timestamp,
                incomingNotification.Title,
                incomingNotification.Body,
                default,
                null);

            NotifySubscribers(notificationDataTransferObject);
            toastNotificationService.Show(incomingNotification.Title, incomingNotification.Body);
        }

        private void NotifySubscribers(NotificationDataTransferObject notificationDataTransferObject)
        {
            // Snapshot under the lock so a concurrent Subscribe/Unsubscribe can't tear
            // the enumeration, then publish outside the lock so a slow observer can't
            // stall incoming subscriptions.
            IObserver<NotificationDataTransferObject>[] snapshot;
            lock (subscribersLock)
            {
                snapshot = subscribers.ToArray();
            }

            foreach (var subscriber in snapshot)
            {
                subscriber.OnNext(notificationDataTransferObject);
            }
        }

        public IDisposable Subscribe(IObserver<NotificationDataTransferObject> observer)
        {
            lock (subscribersLock)
            {
                subscribers.Add(observer);
            }

            return new Unsubscriber(subscribers, subscribersLock, observer);
        }

        private sealed class Unsubscriber : IDisposable
        {
            private readonly List<IObserver<NotificationDataTransferObject>> subscribers;
            private readonly object subscribersLock;
            private readonly IObserver<NotificationDataTransferObject> observer;

            public Unsubscriber(
                List<IObserver<NotificationDataTransferObject>> subscribers,
                object subscribersLock,
                IObserver<NotificationDataTransferObject> observer)
            {
                this.subscribers = subscribers;
                this.subscribersLock = subscribersLock;
                this.observer = observer;
            }

            public void Dispose()
            {
                lock (subscribersLock)
                {
                    subscribers.Remove(observer);
                }
            }
        }

        public void SubscribeToServer(int userIdentifier) => serverClient.SubscribeToServer(userIdentifier);

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            scheduleCancellationTokenSource.Cancel();
            scheduleCancellationTokenSource.Dispose();
            StopListening();
            (serverClient as IDisposable)?.Dispose();
        }

        public void ScheduleUpcomingRentalReminder(int renterIdentifier, int ownerIdentifier, string gameName, DateTime startDate)
        {
            DateTime scheduledTime = startDate.AddDays(-ReminderLeadDays);
            string title = Constants.NotificationTitles.UpcomingRentalReminder;
            string body = CreateReminderBody(gameName, startDate);

            ScheduleOrSendUserNotification(renterIdentifier, title, body, scheduledTime);
            ScheduleOrSendUserNotification(ownerIdentifier, title, body, scheduledTime);
        }

        private static string CreateReminderBody(string gameName, DateTime startDate)
        {
            return $"Game: {gameName}\nStart: {startDate:dd/MM/yyyy HH:mm}\n" +
                   "Delivery/Pick-up: Coordinate delivery/pick-up directly with the other party.";
        }

        private void ScheduleOrSendUserNotification(int userIdentifier, string title, string body, DateTime scheduledTime)
        {
            if (userIdentifier == MissingUserIdentifier)
            {
                return;
            }

            TimeSpan delay = scheduledTime.ToUniversalTime() - DateTime.UtcNow;

            var notificationDataTransferObject = BuildNotificationDataTransferObject(
                NewEntityIdentifier,
                userIdentifier,
                scheduledTime,
                title,
                body,
                default,
                null);

            if (delay <= TimeSpan.Zero)
            {
                SendNotificationToUser(userIdentifier, notificationDataTransferObject);
                return;
            }

            notificationRepository.Add(BuildNotificationModel(userIdentifier, scheduledTime, title, body));

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delay, scheduleCancellationTokenSource.Token);
                    serverClient.SendNotification(userIdentifier, title, body);
                    toastNotificationService.Show(title, body);
                }
                catch (OperationCanceledException)
                {
                    // Disposal/shutdown raced the scheduled send — ignore.
                }
                catch (Exception scheduledNotificationException)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"NotificationService: scheduled reminder failed — {scheduledNotificationException}");
                }
            });
        }

        private static NotificationDataTransferObject BuildNotificationDataTransferObject(
            int notificationIdentifier,
            int userIdentifier,
            DateTime timestamp,
            string title,
            string body,
            NotificationType type,
            int? relatedRequestIdentifier)
        {
            return new NotificationDataTransferObject
            {
                Identifier = notificationIdentifier,
                User = new UserDataTransferObject { Identifier = userIdentifier },
                Timestamp = timestamp,
                Title = title,
                Body = body,
                Type = type,
                RelatedRequestIdentifier = relatedRequestIdentifier
            };
        }

        private static Notification BuildNotificationModel(
            int userIdentifier,
            DateTime timestamp,
            string title,
            string body,
            NotificationType type = default,
            int? relatedRequestIdentifier = null)
        {
            return new Notification
            {
                Identifier = NewEntityIdentifier,
                User = new User { Identifier = userIdentifier },
                Timestamp = timestamp,
                Title = title,
                Body = body,
                Type = type,
                RelatedRequestIdentifier = relatedRequestIdentifier
            };
        }
    }
}



