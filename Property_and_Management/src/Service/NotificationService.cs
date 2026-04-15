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
    public class NotificationService : INotificationService, IObserver<IncomingNotification>, IObservable<NotificationDTO>, IDisposable
    {
        private static readonly TimeSpan ReminderLeadTime = TimeSpan.FromHours(24);
        private const int NewEntityId = 0;
        private const int MissingUserId = 0;

        private bool disposed;
        private readonly CancellationTokenSource scheduleCancellationTokenSource = new();
        private readonly INotificationRepository notificationRepository;
        private readonly IMapper<Notification, NotificationDTO> notificationMapper;
        private readonly IServerClient serverClient;
        private readonly ICurrentUserContext currentUserContext;
        private readonly IToastNotificationService toastNotificationService;

        private readonly List<IObserver<NotificationDTO>> subscribers = new();
        private readonly object subscribersLock = new();

        public NotificationService(
            INotificationRepository notificationRepository,
            IMapper<Notification, NotificationDTO> notificationMapper,
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

        public NotificationDTO DeleteNotificationByIdentifier(int notificationId) =>
            notificationMapper.ToDTO(notificationRepository.Delete(notificationId));

        public NotificationDTO GetNotificationByIdentifier(int notificationId) =>
            notificationMapper.ToDTO(notificationRepository.Get(notificationId));

        public ImmutableList<NotificationDTO> GetNotificationsForUser(int userId) =>
            notificationRepository
                .GetNotificationsByUser(userId)
                .Select(notification => notificationMapper.ToDTO(notification))
                .ToImmutableList();

        public void SendNotificationToUser(int userId, NotificationDTO notification)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            DateTime timestamp = notification.Timestamp == default ? DateTime.UtcNow : notification.Timestamp;

            var notificationModel = BuildNotificationModel(
                userId,
                timestamp,
                notification.Title,
                notification.Body,
                notification.Type,
                notification.RelatedRequestId);

            notificationRepository.Add(notificationModel);

            if (currentUserContext.currentUserId == userId)
            {
                NotifySubscribers(BuildNotificationDTO(
                    notificationModel.Id,
                    userId,
                    timestamp,
                    notification.Title,
                    notification.Body,
                    notification.Type,
                    notification.RelatedRequestId));
            }

            serverClient.SendNotification(userId, notification.Title, notification.Body);
        }

        public void DeleteNotificationsLinkedToRequest(int relatedRequestId)
        {
            notificationRepository.DeleteNotificationsLinkedToRequest(relatedRequestId);
        }

        public void UpdateNotificationByIdentifier(int notificationId, NotificationDTO notification)
        {
            notificationRepository.Update(notificationId, notificationMapper.ToModel(notification));
        }

        public void StartListening()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await serverClient.ListenAsync();
                }
                catch (Exception listenException)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"NotificationService: listen loop terminated - {listenException}");
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
            var NotificationDTO = BuildNotificationDTO(
                NewEntityId,
                incomingNotification.UserId,
                incomingNotification.Timestamp,
                incomingNotification.Title,
                incomingNotification.Body,
                default,
                null);

            NotifySubscribers(NotificationDTO);
            toastNotificationService.Show(incomingNotification.Title, incomingNotification.Body);
        }

        private void NotifySubscribers(NotificationDTO NotificationDTO)
        {
            IObserver<NotificationDTO>[] snapshot;
            lock (subscribersLock)
            {
                snapshot = subscribers.ToArray();
            }

            foreach (var subscriber in snapshot)
            {
                subscriber.OnNext(NotificationDTO);
            }
        }

        public IDisposable Subscribe(IObserver<NotificationDTO> observer)
        {
            lock (subscribersLock)
            {
                subscribers.Add(observer);
            }

            return new Unsubscriber(subscribers, subscribersLock, observer);
        }

        private sealed class Unsubscriber : IDisposable
        {
            private readonly List<IObserver<NotificationDTO>> subscribers;
            private readonly object subscribersLock;
            private readonly IObserver<NotificationDTO> observer;

            public Unsubscriber(
                List<IObserver<NotificationDTO>> subscribers,
                object subscribersLock,
                IObserver<NotificationDTO> observer)
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

        public void SubscribeToServer(int userId) => serverClient.SubscribeToServer(userId);

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

        public void ScheduleUpcomingRentalReminder(int renterId, int ownerId, string gameName, DateTime startDate)
        {
            var rentalStartTime = startDate.ToUniversalTime();
            var currentTime = DateTime.UtcNow;

            if (rentalStartTime <= currentTime)
            {
                return;
            }

            DateTime scheduledTime = rentalStartTime - ReminderLeadTime;
            string title = Constants.NotificationTitles.UpcomingRentalReminder;
            string body = CreateReminderBody(gameName, startDate);

            ScheduleOrSendUserNotification(renterId, title, body, scheduledTime);
            ScheduleOrSendUserNotification(ownerId, title, body, scheduledTime);
        }

        private static string CreateReminderBody(string gameName, DateTime startDate)
        {
            return $"Game: {gameName}\nStart: {startDate:dd/MM/yyyy HH:mm}\n" +
                   "Delivery/Pick-up: Coordinate delivery/pick-up directly with the other party.";
        }

        private void ScheduleOrSendUserNotification(int userId, string title, string body, DateTime scheduledTime)
        {
            if (userId == MissingUserId)
            {
                return;
            }

            TimeSpan delay = scheduledTime.ToUniversalTime() - DateTime.UtcNow;
            if (delay <= TimeSpan.Zero)
            {
                SendReminderNotificationNow(userId, title, body);
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delay, scheduleCancellationTokenSource.Token);
                    SendReminderNotificationNow(userId, title, body);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception scheduledNotificationException)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"NotificationService: scheduled reminder failed - {scheduledNotificationException}");
                }
            });
        }

        private void SendReminderNotificationNow(int userId, string title, string body)
        {
            var reminderNotification = BuildNotificationDTO(
                NewEntityId,
                userId,
                DateTime.UtcNow,
                title,
                body,
                default,
                null);

            SendNotificationToUser(userId, reminderNotification);
        }

        private static NotificationDTO BuildNotificationDTO(
            int notificationId,
            int userId,
            DateTime timestamp,
            string title,
            string body,
            NotificationType type,
            int? relatedRequestId)
        {
            return new NotificationDTO
            {
                Id = notificationId,
                User = new UserDTO { Id = userId },
                Timestamp = timestamp,
                Title = title,
                Body = body,
                Type = type,
                RelatedRequestId = relatedRequestId
            };
        }

        private static Notification BuildNotificationModel(
            int userId,
            DateTime timestamp,
            string title,
            string body,
            NotificationType type = default,
            int? relatedRequestId = null)
        {
            return new Notification
            {
                Id = NewEntityId,
                User = new User { Id = userId },
                Timestamp = timestamp,
                Title = title,
                Body = body,
                Type = type,
                RelatedRequestId = relatedRequestId
            };
        }
    }
}
