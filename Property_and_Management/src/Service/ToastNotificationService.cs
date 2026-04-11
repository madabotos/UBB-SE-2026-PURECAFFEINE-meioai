using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Property_and_Management.Src.Interface;

namespace Property_and_Management.Src.Service
{
    public class ToastNotificationService : IToastNotificationService
    {
        // Must match the key checked in App.xaml.cs NotificationClicked handler
        private const string NavigationKey = "navigate";
        private const string NotificationsPageKey = "NotificationsPage";

        public void Show(string title, string body)
        {
            var notification = new AppNotificationBuilder()
                .AddArgument(NavigationKey, NotificationsPageKey)
                .AddText(title)
                .AddText(body)
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }
    }
}
