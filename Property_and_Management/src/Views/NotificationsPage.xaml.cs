using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management.src.DataTransferObjects;
using Property_and_Management.src.Viewmodels;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Property_and_Management.src.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NotificationsPage : Page
    {
        private const int NotificationsRefreshIntervalSeconds = 10;
        private readonly DispatcherTimer _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(NotificationsRefreshIntervalSeconds) };

        public NotificationsPage()
        {
            InitializeComponent();

            // Grab the ViewModel straight from the App!
            var applicationInstance = (Property_and_Management.App)Application.Current;
            this.DataContext = applicationInstance.NotificationsViewModel;

            _refreshTimer.Tick += (timerSender, tickEventArgs) =>
            {
                if (DataContext is NotificationsViewModel notificationsViewModel)
                    notificationsViewModel.LoadNotificationsForUser(notificationsViewModel.CurrentUserIdentifier);
            };
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is NotificationsViewModel notificationsViewModel)
            {
                DataContext = notificationsViewModel;
                notificationsViewModel.LoadNotificationsForUser(notificationsViewModel.CurrentUserIdentifier);

                if (this.FindName("ItemsListView") is ItemsControl items)
                    items.ItemsSource = notificationsViewModel.Notifications;
            }
            else if (DataContext is NotificationsViewModel defaultNotificationsViewModel)
            {
                defaultNotificationsViewModel.LoadNotificationsForUser(defaultNotificationsViewModel.CurrentUserIdentifier);
            }

            _refreshTimer.Start();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            _refreshTimer.Stop();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var clickedButton = sender as Button;
                // In ItemsControl the DataContext for the button is the NotificationDataTransferObject
                var notification = clickedButton?.DataContext as NotificationDataTransferObject;
                if (notification == null)
                {
                    Debug.WriteLine("DeleteButton_Click: notification Data Transfer Object not found");
                    return;
                }

                var root = this.Content as FrameworkElement;
                var notificationsViewModel = root?.DataContext as NotificationsViewModel;
                if (notificationsViewModel == null)
                {
                    Debug.WriteLine("NotificationsPage: viewmodel not found on DeleteButton_Click");
                    return;
                }

                notificationsViewModel.DeleteNotificationByIdentifier(notification.Identifier);
            }
            catch (Exception caughtException)
            {
                Debug.WriteLine($"DeleteButton_Click error: {caughtException}");
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            var root = this.Content as FrameworkElement;
            var notificationsViewModel = root?.DataContext as NotificationsViewModel;
            notificationsViewModel?.NextPage();
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            var root = this.Content as FrameworkElement;
            var notificationsViewModel = root?.DataContext as NotificationsViewModel;
            notificationsViewModel?.PrevPage();
        }
    }
}


