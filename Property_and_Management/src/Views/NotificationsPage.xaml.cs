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
using Property_and_Management.src.DTO;
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
        public NotificationsPage()
        {
            InitializeComponent();

            // Grab the ViewModel straight from the App!
            var app = (Property_and_Management.App)Application.Current;
            this.DataContext = app.NotificationsViewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is NotificationsViewModel vm)
            {
                DataContext = vm;

                // If the XAML contains a named ItemsControl / ListView called "ItemsListView" from older implementation,
                // bind its ItemsSource to the VM's Notifications collection to preserve behavior.
                if (this.FindName("ItemsListView") is ItemsControl items)
                {
                    items.ItemsSource = vm.Notifications;
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var btn = sender as Button;
                // In ItemsControl the DataContext for the button is the NotificationDTO
                var note = btn?.DataContext as NotificationDTO;
                if (note == null)
                {
                    Debug.WriteLine("DeleteButton_Click: notification DTO not found");
                    return;
                }

                var root = this.Content as FrameworkElement;
                var vm = root?.DataContext as NotificationsViewModel;
                if (vm == null)
                {
                    Debug.WriteLine("NotificationsPage: viewmodel not found on DeleteButton_Click");
                    return;
                }

                vm.DeleteNotificationById(note.Id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DeleteButton_Click error: {ex}");
            }
        }

        private async void ApproveOffer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var btn = sender as Button;
                var note = btn?.DataContext as NotificationDTO;
                if (note?.RelatedRequestId == null) return;

                var root = this.Content as FrameworkElement;
                var vm = root?.DataContext as NotificationsViewModel;
                if (vm == null) return;

                var result = vm.ApproveOffer(note.RelatedRequestId.Value);
                if (result < 0)
                {
                    string message = result switch
                    {
                        -1 => "Request not found.",
                        -2 => "You are not authorized for this action.",
                        -3 => "No pending offer to approve.",
                        -4 => "Transaction failed. Please try again.",
                        _ => "An unexpected error occurred."
                    };
                    var dialog = new ContentDialog
                    {
                        Title = "Approve Failed",
                        Content = message,
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApproveOffer_Click error: {ex}");
            }
        }

        private async void DenyOffer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var btn = sender as Button;
                var note = btn?.DataContext as NotificationDTO;
                if (note?.RelatedRequestId == null) return;

                var root = this.Content as FrameworkElement;
                var vm = root?.DataContext as NotificationsViewModel;
                if (vm == null) return;

                var result = vm.DenyOffer(note.RelatedRequestId.Value);
                if (result < 0)
                {
                    string message = result switch
                    {
                        -1 => "Request not found.",
                        -2 => "You are not authorized for this action.",
                        -3 => "No pending offer to deny.",
                        _ => "An unexpected error occurred."
                    };
                    var dialog = new ContentDialog
                    {
                        Title = "Deny Failed",
                        Content = message,
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DenyOffer_Click error: {ex}");
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            var root = this.Content as FrameworkElement;
            var vm = root?.DataContext as NotificationsViewModel;
            vm?.NextPage();
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            var root = this.Content as FrameworkElement;
            var vm = root?.DataContext as NotificationsViewModel;
            vm?.PrevPage();
        }
    }
}
