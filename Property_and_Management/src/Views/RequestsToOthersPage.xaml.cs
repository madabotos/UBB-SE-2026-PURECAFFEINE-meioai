using System;
using Microsoft.Extensions.DependencyInjection;
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
                return;
            }

            if (DataContext is not RequestsToOthersViewModel)
            {
                DataContext = App.Services.GetRequiredService<RequestsToOthersViewModel>();
            }
        }

        private void RequestItem_Tapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is RequestDTO request && request.Id > 0)
                Frame?.Navigate(typeof(ChatView), request.Id);
        }

        private void RequestItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is RequestDTO request && request.Id > 0)
                Frame?.Navigate(typeof(ChatView), request.Id);
        }

        private void CancelButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not int requestId)
                return;

            ContentDialog cancelDialog = new ContentDialog
            {
                Title = "Cancel Request?",
                Content = "Are you sure you want to cancel this request?",
                PrimaryButtonText = "Cancel Request",
                CloseButtonText = "Go Back",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await cancelDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var vm = DataContext as RequestsToOthersViewModel;
                vm?.CancelRequest(requestId);
            }
        }

        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (sender is not Image img) return;

            if (img.Source is BitmapImage current &&
                current.UriSource != null &&
                current.UriSource.AbsoluteUri.EndsWith("/Assets/default-game-placeholder.jpg", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (Resources.TryGetValue("DefaultGameImage", out var localResource) && localResource is BitmapImage localImage)
            {
                img.Source = localImage;
                return;
            }

            if (Application.Current.Resources.TryGetValue("DefaultGameImage", out var appResource) && appResource is BitmapImage appImage)
            {
                img.Source = appImage;
                return;
            }

            img.Source = new BitmapImage(new Uri("ms-appx:///Assets/default-game-placeholder.jpg"));
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as RequestsToOthersViewModel)?.NextPage();
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as RequestsToOthersViewModel)?.PrevPage();
        }
    }
}
