using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Service
{
    public class NotificationService : INotificationService, IObserver<IncomingNotification>, IObservable<NotificationDTO>, IDisposable
    {
        private bool _disposed;
        private readonly CancellationTokenSource _scheduleCts = new();
        private readonly INotificationRepository _notificationRepository;
        private readonly IMapper<Notification, NotificationDTO> _notificationMapper;
        private readonly IServerClient _serverClient;
        private readonly ICurrentUserContext _currentUserContext;
        private readonly IToastNotificationService _toastNotificationService;
        private List<IObserver<NotificationDTO>> _subscribers = [];

        public NotificationService(
            INotificationRepository notificationRepository,
            IMapper<Notification, NotificationDTO> notificationMapper,
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

        public NotificationDTO DeleteNotificationById(int id) =>
            _notificationMapper.ToDTO(_notificationRepository.Delete(id));

        public NotificationDTO GetNotificationById(int id) =>
            _notificationMapper.ToDTO(_notificationRepository.Get(id));

        public ImmutableList<NotificationDTO> GetNotificationsForUser(int userId) =>
            _notificationRepository
                .GetNotificationsByUser(userId)
                .Select(entity => _notificationMapper.ToDTO(entity))
                .ToImmutableList();

        public void SendNotificationToUser(int userId, NotificationDTO notification)
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));

            DateTime timestamp = notification.Timestamp == default ? DateTime.UtcNow : notification.Timestamp;

            var notificationModel = new Notification
            {
                Id = 0,
                User = new User { Id = userId },
                Timestamp = timestamp,
                Title = notification.Title,
                Body = notification.Body,
                Type = notification.Type,
                RelatedRequestId = notification.RelatedRequestId
            };

            _notificationRepository.Add(notificationModel);

            if (_currentUserContext.CurrentUserId == userId)
            {
                NotifySubscribers(new NotificationDTO
                {
                    Id = notificationModel.Id,
                    User = new UserDTO { Id = userId },
                    Timestamp = timestamp,
                    Title = notification.Title,
                    Body = notification.Body,
                    Type = notification.Type,
                    RelatedRequestId = notification.RelatedRequestId
                });
            }

            _serverClient.SendNotification(userId, notification.Title, notification.Body);
        }

        public void DeleteNotificationsByRequestId(int requestId)
        {
            _notificationRepository.DeleteByRequestId(requestId);
        }

        public void UpdateNotificationById(int id, NotificationDTO notification)
        {
            _notificationRepository.Update(id, _notificationMapper.ToModel(notification));
        }

        public void StartListening() => _serverClient.ListenAsync();
        public void StopListening() => _serverClient.StopListening();
        public void OnCompleted() { }
        public void OnError(Exception error) { }

        public void OnNext(IncomingNotification value)
        {
            var notificationDTO = new NotificationDTO
            {
                User = new UserDTO { Id = value.UserId },
                Timestamp = value.Timestamp,
                Title = value.Title,
                Body = value.Body
            };

            NotifySubscribers(notificationDTO);
            _toastNotificationService.Show(value.Title, value.Body);
        }

        private void NotifySubscribers(NotificationDTO notificationDTO)
        {
            foreach (var subscriber in _subscribers.ToArray())
                subscriber.OnNext(notificationDTO);
        }

        public IDisposable Subscribe(IObserver<NotificationDTO> observer)
        {
            _subscribers.Add(observer);
            return new Unsubscriber(_subscribers, observer);
        }

        private sealed class Unsubscriber : IDisposable
        {
            private readonly List<IObserver<NotificationDTO>> _subscribers;
            private readonly IObserver<NotificationDTO> _observer;

            public Unsubscriber(List<IObserver<NotificationDTO>> subscribers, IObserver<NotificationDTO> observer)
            {
                _subscribers = subscribers;
                _observer = observer;
            }

            public void Dispose() => _subscribers.Remove(_observer);
        }

        public void SubscribeToServer(int userId) => _serverClient.SubscribeToServer(userId);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _scheduleCts.Cancel();
            _scheduleCts.Dispose();
            StopListening();
            (_serverClient as IDisposable)?.Dispose();
        }

        public void ScheduleUpcomingRentalReminder(int renterId, int ownerId, string gameName, DateTime startDate)
        {
            DateTime scheduledTime = startDate.AddDays(-1);
            string title = "Upcoming Rental Reminder";
            string body = $"Game: {gameName}\nStart: {startDate:dd/MM/yyyy HH:mm}\n" +
                          "Delivery/Pick-up: Coordinate delivery/pick-up directly with the other party.";

            ScheduleOrSendUserNotification(renterId, title, body, scheduledTime);
            ScheduleOrSendUserNotification(ownerId, title, body, scheduledTime);
        }

        private void ScheduleOrSendUserNotification(int userId, string title, string body, DateTime scheduledTime)
        {
            if (userId == 0) return;

            TimeSpan delay = scheduledTime.ToUniversalTime() - DateTime.UtcNow;

            var dto = new NotificationDTO
            {
                Timestamp = scheduledTime,
                Title = title,
                Body = body,
                User = new UserDTO { Id = userId }
            };

            if (delay <= TimeSpan.Zero)
            {
                SendNotificationToUser(userId, dto);
                return;
            }

            _notificationRepository.Add(new Notification
            {
                Id = 0,
                User = new User { Id = userId },
                Timestamp = scheduledTime,
                Title = title,
                Body = body
            });

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delay, _scheduleCts.Token);
                    _serverClient.SendNotification(userId, title, body);
                    _toastNotificationService.Show(title, body);
                }
                catch (OperationCanceledException) { }
                catch { }
            });
        }
    }
}
