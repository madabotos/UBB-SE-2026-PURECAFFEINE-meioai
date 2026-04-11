using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Property_and_Management;
using Property_and_Management.src.DataTransferObjects;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Service
{
    public class NotificationService : INotificationService, IObserver<IncomingNotification>, IObservable<NotificationDataTransferObject>, IDisposable
    {
        private const int ReminderLeadDays = 1;
        private const int NewEntityIdentifier = 0;
        private const int MissingUserIdentifier = 0;

        private bool _disposed;
        private readonly CancellationTokenSource _scheduleCancellationTokenSource = new();
        private readonly INotificationRepository _notificationRepository;
        private readonly IMapper<Notification, NotificationDataTransferObject> _notificationMapper;
        private readonly IServerClient _serverClient;
        private readonly ICurrentUserContext _currentUserContext;
        private readonly IToastNotificationService _toastNotificationService;
        private List<IObserver<NotificationDataTransferObject>> _subscribers = [];

        public NotificationService(
            INotificationRepository notificationRepository,
            IMapper<Notification, NotificationDataTransferObject> notificationMapper,
            IServerClient serverClient,
            ICurrentUserContext currentUserContext,
            IToastNotificationService toastNotificationService)
        {
            _notificationRepository = notificationRepository;
            _notificationMapper = notificationMapper;
            _serverClient = serverClient;
            _currentUserContext = currentUserContext;
            _toastNotificationService = toastNotificationService;
            _serverClient.Subscribe(this);
        }

        public NotificationDataTransferObject DeleteNotificationByIdentifier(int notificationIdentifier) =>
            _notificationMapper.ToDataTransferObject(_notificationRepository.Delete(notificationIdentifier));

        public NotificationDataTransferObject GetNotificationByIdentifier(int notificationIdentifier) =>
            _notificationMapper.ToDataTransferObject(_notificationRepository.Get(notificationIdentifier));

        public ImmutableList<NotificationDataTransferObject> GetNotificationsForUser(int userIdentifier) =>
            _notificationRepository
                .GetNotificationsByUser(userIdentifier)
                .Select(notification => _notificationMapper.ToDataTransferObject(notification))
                .ToImmutableList();

        public void SendNotificationToUser(int userIdentifier, NotificationDataTransferObject notification)
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));

            DateTime timestamp = notification.Timestamp == default ? DateTime.UtcNow : notification.Timestamp;

            var notificationModel = BuildNotificationModel(
                userIdentifier,
                timestamp,
                notification.Title,
                notification.Body,
                notification.Type,
                notification.RelatedRequestIdentifier);

            _notificationRepository.Add(notificationModel);

            if (_currentUserContext.CurrentUserIdentifier == userIdentifier)
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

            _serverClient.SendNotification(userIdentifier, notification.Title, notification.Body);
        }

        public void DeleteNotificationsByRequestId(int requestIdentifier)
        {
            _notificationRepository.DeleteByRequestId(requestIdentifier);
        }

        public void UpdateNotificationByIdentifier(int notificationIdentifier, NotificationDataTransferObject notification)
        {
            _notificationRepository.Update(notificationIdentifier, _notificationMapper.ToModel(notification));
        }

        public void StartListening() => _serverClient.ListenAsync();
        public void StopListening() => _serverClient.StopListening();
        public void OnCompleted() { }
        public void OnError(Exception error) { }

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
            _toastNotificationService.Show(incomingNotification.Title, incomingNotification.Body);
        }

        private void NotifySubscribers(NotificationDataTransferObject notificationDataTransferObject)
        {
            foreach (var subscriber in _subscribers.ToArray())
                subscriber.OnNext(notificationDataTransferObject);
        }

        public IDisposable Subscribe(IObserver<NotificationDataTransferObject> observer)
        {
            _subscribers.Add(observer);
            return new Unsubscriber(_subscribers, observer);
        }

        private sealed class Unsubscriber : IDisposable
        {
            private readonly List<IObserver<NotificationDataTransferObject>> _subscribers;
            private readonly IObserver<NotificationDataTransferObject> _observer;

            public Unsubscriber(List<IObserver<NotificationDataTransferObject>> subscribers, IObserver<NotificationDataTransferObject> observer)
            {
                _subscribers = subscribers;
                _observer = observer;
            }

            public void Dispose() => _subscribers.Remove(_observer);
        }

        public void SubscribeToServer(int userIdentifier) => _serverClient.SubscribeToServer(userIdentifier);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _scheduleCancellationTokenSource.Cancel();
            _scheduleCancellationTokenSource.Dispose();
            StopListening();
            (_serverClient as IDisposable)?.Dispose();
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
            if (userIdentifier == MissingUserIdentifier) return;

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

            _notificationRepository.Add(BuildNotificationModel(userIdentifier, scheduledTime, title, body));

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delay, _scheduleCancellationTokenSource.Token);
                    _serverClient.SendNotification(userIdentifier, title, body);
                    _toastNotificationService.Show(title, body);
                }
                catch (OperationCanceledException) { }
                catch { }
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



