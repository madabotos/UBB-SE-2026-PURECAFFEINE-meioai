using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;
using Property_and_Management.src.Repository;
using Property_and_Management.src.Service.Listeners;
using Property_and_Management.src.Views;
using ServerCommunication;
using Windows.UI.Notifications;

namespace Property_and_Management.src.Service
{
    public class NotificationService : INotificationService, IObserver<MessageBase>, IObservable<NotificationDTO>
    {
        private readonly NotificationRepository _notificationRepository;

        private IServerClient _serverClient;
        private List<IObserver<NotificationDTO>> _subscribers = [];

        public NotificationService(NotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
            _serverClient = new NotificationClient();

            _serverClient.Subscribe(this);
        }


        public NotificationDTO DeleteNotificationById(int id)
        {
            return (NotificationDTO)NotificationDTO.FromModel(_notificationRepository.Delete(id));
        }

        public NotificationDTO GetNotificationById(int id)
        {
            return (NotificationDTO)NotificationDTO.FromModel(_notificationRepository.Get(id));
        }

        public ImmutableList<NotificationDTO> GetNotificationsForUser(int userId)
        {
            return _notificationRepository
                .GetNotificationsByUser(userId)
                .Select(entity => (NotificationDTO)NotificationDTO.FromModel(entity))
                .ToImmutableList();
        }

        // Keep the existing DTO-based overload but persist before sending
        public void SendNotificationToUser(int userId, NotificationDTO notification)
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));

            // Ensure timestamp is set
            DateTime timestamp = notification.Timestamp == default ? DateTime.UtcNow : notification.Timestamp;

            // Persist the notification into repository using the model type (qualify to avoid ambiguity)
            var notificationModel = new Property_and_Management.src.Model.Notification(0, new User(userId), timestamp, notification.Title, notification.Body);
            _notificationRepository.Add(notificationModel);

            // Send via server client so remote subscriber (app instance) receives the message
            _serverClient.SendNotification(userId, notification.Title, notification.Body);
        }

        public void UpdateNotificationById(int id, NotificationDTO notification)
        {
            _notificationRepository.Update(id, notification.ToModel());
        }

        public void StartListening()
        {
            _serverClient.ListenAsync();
        }

        public void StopListening()
        {
            _serverClient.StopListening();
        }

        public void OnCompleted()
        {
            // throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            // throw new NotImplementedException();
        }

        public void OnNext(MessageBase value)
        {
            // Notify all subscribers
            // only SendNotificationMessage is supported
            if (value is SendNotificationMessage message)
            {
                NotificationDTO notificationDTO = new NotificationDTO
                {
                    Timestamp = message.Timestamp,
                    Title = message.Title,
                    Body = message.Body,
                };

                foreach (var subscriber in _subscribers)
                {
                    subscriber.OnNext(notificationDTO);
                }

                // Display a system notification
                ShowWindowsNotification(message.Title, message.Body);
            }
        }

        public IDisposable Subscribe(IObserver<NotificationDTO> observer)
        {
            _subscribers.Add(observer);
            return null;
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

        public void SubscribeToServer(int userId)
        {
            _serverClient.SubscribeToServer(userId);
        }

        // REQ-NOT-01: Schedule Upcoming Rental Reminder notifications 24 hours before rental start
        public void ScheduleUpcomingRentalReminder(Rental rental)
        {
            if (rental == null) throw new ArgumentNullException(nameof(rental));

            DateTime scheduledTime = rental.StartDate.AddDays(-1);

            string title = "Upcoming Rental Reminder";
            string body = ComposeUpcomingRentalBody(rental);

            // Do NOT persist here. ScheduleOrSendUserNotification will call SendNotificationToUser which persists when sending.
            ScheduleOrSendUserNotification(rental.Renter?.Id ?? 0, title, body, scheduledTime);
            ScheduleOrSendUserNotification(rental.Owner?.Id ?? 0, title, body, scheduledTime);
        }

        private string ComposeUpcomingRentalBody(Rental rental)
        {
            var gameName = rental.Game?.Name ?? "your game";
            var start = rental.StartDate.ToString("yyyy-MM-dd HH:mm");
            var pickupInstructions = GetPickupInstructions(rental);

            return $"{gameName} rental starts on {start}. Pickup/delivery instructions: {pickupInstructions}";
        }

        private string GetPickupInstructions(Rental rental)
        {
            // Placeholder instructions — integration with a logistics/communication subsystem could replace this
            return "Please coordinate pickup or delivery with the other party (check messages for contact details).";
        }

        private void ScheduleOrSendUserNotification(int userId, string title, string body, DateTime scheduledTime)
        {
            if (userId == 0) return; // invalid user

            DateTime utcNow = DateTime.UtcNow;
            // Convert scheduledTime to UTC if it is local; assuming stored dates are UTC or unspecified, we compare as-is
            TimeSpan delay = scheduledTime.ToUniversalTime() - utcNow;

            if (delay <= TimeSpan.Zero)
            {
                // send immediately and persist via SendNotificationToUser
                var dto = new NotificationDTO { Timestamp = scheduledTime, Title = title, Body = body, User = new User(userId) };
                SendNotificationToUser(userId, dto);
                return;
            }

            // Schedule in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delay);
                    var dto = new NotificationDTO { Timestamp = scheduledTime, Title = title, Body = body, User = new User(userId) };
                    SendNotificationToUser(userId, dto);
                }
                catch (Exception)
                {
                    // Swallow exceptions to avoid crashing background task. In production, consider logging and retrying.
                }
            });
        }
    }
}
