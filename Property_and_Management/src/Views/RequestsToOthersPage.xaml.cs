using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Repository;
using Property_and_Management.src.Service;
using Property_and_Management.src.Viewmodels;

namespace Property_and_Management.src.Views
{
    public sealed partial class RequestsToOthersPage : Page
    {
        public RequestsToOthersPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is RequestsToOthersViewModel vm)
            {
                DataContext = vm;

                if (this.FindName("ItemsListView") is ItemsControl items)
                {
                    items.ItemsSource = vm.PagedRequests;
                }

                return;
            }

            if (DataContext is not RequestsToOthersViewModel)
            {
                var requestService = new RequestService();
                requestService.SetRequestRepository(new RequestRepository());

                var fallbackVm = new RequestsToOthersViewModel(requestService);
                DataContext = fallbackVm;

                if (this.FindName("ItemsListView") is ItemsControl items)
                {
                    items.ItemsSource = fallbackVm.PagedRequests;
                }
            }
        }

        private void RequestItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                var grid = sender as Grid;
                var request = grid?.DataContext as RequestDTO;
                if (request?.Id > 0)
                {
                    Frame?.Navigate(typeof(ChatView), request.Id);  // [UI-MRQ-05]
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"RequestItem_Tapped error: {ex}");
            }
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var btn = sender as Button;
                var request = btn?.DataContext as RequestDTO;
                if (request == null) return;

                // [UI-MRQ-04]
                var dialog = new ContentDialog()
                {
                    Title = "Confirm Cancel",
                    Content = $"Cancel request for {request.Game.Name} ({request.StartDate:dd/MM} - {request.EndDate:dd/MM})?",
                    PrimaryButtonText = "Cancel Request",
                    CloseButtonText = "Keep",
                    DefaultButton = ContentDialogButton.Primary
                };

                var dialogResult = await dialog.ShowAsync();
                if (dialogResult == ContentDialogResult.Primary)
                {
                    var root = this.Content as FrameworkElement;
                    var vm = root?.DataContext as RequestsToOthersViewModel;
                    vm?.CancelRequest(request.Id);
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"CancelButton_Click error: {ex}");
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            var root = this.Content as FrameworkElement;
            var vm = root?.DataContext as RequestsToOthersViewModel;
            vm?.NextPage();
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            var root = this.Content as FrameworkElement;
            var vm = root?.DataContext as RequestsToOthersViewModel;
            vm?.PrevPage();
        }
    }
}
