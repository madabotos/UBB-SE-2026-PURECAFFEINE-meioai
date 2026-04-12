using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Windows.AppNotifications;

namespace Property_and_Management
{
    internal class NotificationManager
    {
        public event EventHandler<AppNotificationActivatedEventArgs> NotificationClicked;

        private bool isRegistered;

        public NotificationManager()
        {
            isRegistered = false;
        }

        public void Init()
        {
            // To ensure all Notification handling happens in this process instance, register for
            // NotificationInvoked before calling Register(). Without this a new process will
            // be launched to handle the notification.
            AppNotificationManager notificationManager = AppNotificationManager.Default;

            notificationManager.NotificationInvoked += OnNotificationInvoked;

            try
            {
                notificationManager.Register();
                isRegistered = true;
            }
            catch (Exception registrationException)
            {
                // This prevents crashes if the app restarts during debugging
                // and Windows thinks it's already registered.
                System.Diagnostics.Debug.WriteLine($"Toast manager failed to register: {registrationException.Message}");
            }
        }

        public void Unregister()
        {
            if (isRegistered)
            {
                AppNotificationManager.Default.NotificationInvoked -= OnNotificationInvoked;
                AppNotificationManager.Default.Unregister();
                isRegistered = false;
            }
        }

        private void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
        {
            // Fire our custom event to let App.xaml.cs handle the UI navigation
            NotificationClicked?.Invoke(this, args);
        }

        public void ProcessLaunchActivationArgs(AppNotificationActivatedEventArgs args)
        {
            NotificationClicked?.Invoke(this, args);
        }
    }
}
