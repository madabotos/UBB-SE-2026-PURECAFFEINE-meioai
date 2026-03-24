using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management.src.DTO;
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
                    Frame?.Navigate(typeof(ChatPage), request.Id);  // [UI-MRQ-05]
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"RequestItem_Tapped error: {ex}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
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

                if (dialog.ShowAsync() == ContentDialogResult.Primary)
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
