using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Viewmodels;

namespace Property_and_Management.Src.Views
{
    /// <summary>
    /// Notifications list. Periodic refresh is
    /// not needed here — <see cref="NotificationsViewModel"/> subscribes to
    /// <c>INotificationService</c> and reloads itself whenever the UDP
    /// listener pushes a new notification.
    /// </summary>
    public sealed partial class NotificationsPage : Page
    {
        public NotificationsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Prefer the view model passed as a navigation parameter (e.g. from
            // MenuBarView's toast-triggered NavigateToNotifications call). Fall
            // back to the DI container so direct menu navigation still works.
            if (e.Parameter is NotificationsViewModel navigatedViewModel)
            {
                DataContext = navigatedViewModel;
                navigatedViewModel.LoadNotificationsForUser(navigatedViewModel.CurrentUserIdentifier);
                return;
            }

            if (DataContext is NotificationsViewModel existingViewModel)
            {
                existingViewModel.LoadNotificationsForUser(existingViewModel.CurrentUserIdentifier);
                return;
            }

            // Composition root: resolve from DI when nothing was passed in.
            var resolvedViewModel = App.Services.GetRequiredService<NotificationsViewModel>();
            DataContext = resolvedViewModel;
            resolvedViewModel.LoadNotificationsForUser(resolvedViewModel.CurrentUserIdentifier);
        }

        private NotificationsViewModel? ResolveViewModel()
        {
            var root = this.Content as FrameworkElement;
            return root?.DataContext as NotificationsViewModel;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as Button;
            if (clickedButton?.DataContext is not NotificationDataTransferObject notification)
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

            notificationsViewModel.DeleteNotificationByIdentifier(notification.Identifier);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e) => ResolveViewModel()?.NextPage();

        private void PrevButton_Click(object sender, RoutedEventArgs e) => ResolveViewModel()?.PrevPage();
    }
}
