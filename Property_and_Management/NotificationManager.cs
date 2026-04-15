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
            AppNotificationManager notificationManager = AppNotificationManager.Default;

            notificationManager.NotificationInvoked += OnNotificationInvoked;

            try
            {
                notificationManager.Register();
                isRegistered = true;
            }
            catch (Exception registrationException)
            {
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
            NotificationClicked?.Invoke(this, args);
        }

        public void ProcessLaunchActivationArgs(AppNotificationActivatedEventArgs args)
        {
            NotificationClicked?.Invoke(this, args);
        }
    }
}