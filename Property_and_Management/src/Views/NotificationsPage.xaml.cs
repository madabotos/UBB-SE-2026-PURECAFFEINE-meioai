using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Viewmodels;

namespace Property_and_Management.Src.Views
{
    public sealed partial class NotificationsPage : Page
    {
        public NotificationsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs navigationEventArgs)
        {
            base.OnNavigatedTo(navigationEventArgs);

            if (navigationEventArgs.Parameter is NotificationsViewModel navigatedViewModel)
            {
                DataContext = navigatedViewModel;
                navigatedViewModel.LoadNotificationsForUser(navigatedViewModel.currentUserId);
                return;
            }

            if (DataContext is NotificationsViewModel existingViewModel)
            {
                existingViewModel.LoadNotificationsForUser(existingViewModel.currentUserId);
                return;
            }

            var resolvedViewModel = App.Services.GetRequiredService<NotificationsViewModel>();
            DataContext = resolvedViewModel;
            resolvedViewModel.LoadNotificationsForUser(resolvedViewModel.currentUserId);
        }

        private NotificationsViewModel? ResolveViewModel()
        {
            var root = this.Content as FrameworkElement;
            return root?.DataContext as NotificationsViewModel;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            var clickedButton = sender as Button;
            if (clickedButton?.DataContext is not NotificationDTO notification)
            {
                Debug.WriteLine("DeleteButton_Click: notification Data Transfer Object not found");
                return;
            }

            var notificationsViewModel = ResolveViewModel();
            if (notificationsViewModel == null)
            {
                Debug.WriteLine("NotificationsPage: viewmodel not found on DeleteButton_Click");
                return;
            }

            notificationsViewModel.DeleteNotificationByIdentifier(notification.Id);
        }

        private void NextButton_Click(object sender, RoutedEventArgs routedEventArgs) => ResolveViewModel()?.NextPage();

        private void PrevButton_Click(object sender, RoutedEventArgs routedEventArgs) => ResolveViewModel()?.PrevPage();
    }
}