using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;
using Property_and_Management.src.Repository;
using Property_and_Management.src.Service.Listeners;
using Property_and_Management.src.Views;
using ServerCommunication;

namespace Property_and_Management.src.Service
{
    public class NotificationService : INotificationService, IObserver<MessageBase>, IObservable<NotificationDTO>
    {
        private readonly NotificationRepository _notificationRepository;
        private readonly IMapper<Notification, NotificationDTO> _notificationMapper;

        private IServerClient _serverClient;
        private readonly object _subscriberLock = new();
        private List<IObserver<NotificationDTO>> _subscribers = [];

        public NotificationService(
            NotificationRepository notificationRepository,
            IMapper<Notification, NotificationDTO> notificationMapper)
        {
            _notificationRepository = notificationRepository;
            _notificationMapper = notificationMapper;
            _serverClient = new NotificationClient();
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
                Body = notification.Body
            };

            _notificationRepository.Add(notificationModel);
            _serverClient.SendNotification(userId, notification.Title, notification.Body);
        }

        public void UpdateNotificationById(int id, NotificationDTO notification)
        {
            _notificationRepository.Update(id, _notificationMapper.ToModel(notification));
        }

        public void StartListening() => _serverClient.ListenAsync();
        public void StopListening() => _serverClient.StopListening();
        public void OnCompleted() { }
        public void OnError(Exception error) { }

        public void OnNext(MessageBase value)
        {
            if (value is SendNotificationMessage message)
            {
                var notificationDTO = new NotificationDTO
                {
                    Timestamp = message.Timestamp,
                    Title = message.Title,
                    Body = message.Body
                };

                IObserver<NotificationDTO>[] snapshot;
                lock (_subscriberLock) { snapshot = _subscribers.ToArray(); }
                foreach (var subscriber in snapshot)
                    subscriber.OnNext(notificationDTO);

                ShowWindowsNotification(message.Title, message.Body);
            }
        }

        public IDisposable Subscribe(IObserver<NotificationDTO> observer)
        {
            lock (_subscriberLock) { _subscribers.Add(observer); }
            return new Unsubscriber(_subscriberLock, _subscribers, observer);
        }

        private sealed class Unsubscriber : IDisposable
        {
            private readonly object _lock;
            private readonly List<IObserver<NotificationDTO>> _observers;
            private readonly IObserver<NotificationDTO> _observer;

            public Unsubscriber(object @lock, List<IObserver<NotificationDTO>> observers, IObserver<NotificationDTO> observer)
            {
                _lock = @lock;
                _observers = observers;
                _observer = observer;
            }

            public void Dispose() { lock (_lock) { _observers.Remove(_observer); } }
        }

        private void ShowWindowsNotification(string title, string body)
        {
            var notification = new AppNotificationBuilder()
                .AddArgument("navigate", nameof(NotificationsPage))
                .AddText(title)
                .AddText(body)
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }

        public void SubscribeToServer(int userId) => _serverClient.SubscribeToServer(userId);

        public void ScheduleUpcomingRentalReminder(Rental rental)
        {
            if (rental == null) throw new ArgumentNullException(nameof(rental));

            DateTime scheduledTime = rental.StartDate.AddDays(-1);
            string title = "Upcoming Rental Reminder";
            string body = ComposeUpcomingRentalBody(rental);

            ScheduleOrSendUserNotification(rental.Renter?.Id ?? 0, title, body, scheduledTime);
            ScheduleOrSendUserNotification(rental.Owner?.Id ?? 0, title, body, scheduledTime);
        }

        private string ComposeUpcomingRentalBody(Rental rental)
        {
            var gameName = rental.Game?.Name ?? "your game";
            var start = rental.StartDate.ToString("yyyy-MM-dd HH:mm");
            return $"{gameName} rental starts on {start}. Pickup/delivery instructions: {GetPickupInstructions(rental)}";
        }

        private string GetPickupInstructions(Rental rental) =>
            "Please coordinate pickup or delivery with the other party (check messages for contact details).";

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

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delay);
                    SendNotificationToUser(userId, dto);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    System.Diagnostics.Debug.WriteLine($"Scheduled notification failed for user {userId}: {ex.Message}");
                }
            });
        }
    }
}
