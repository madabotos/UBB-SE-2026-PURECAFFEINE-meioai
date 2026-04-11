using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management;
using Property_and_Management.src.DataTransferObjects;
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

            if (e.Parameter is RequestsToOthersViewModel requestsToOthersViewModel)
            {
                DataContext = requestsToOthersViewModel;
                return;
            }

            if (DataContext is not RequestsToOthersViewModel)
            {
                DataContext = App.Services.GetRequiredService<RequestsToOthersViewModel>();
            }
        }

        private void CancelButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button clickedButton || clickedButton.Tag is not int requestIdentifier)
                return;

            var result = await DialogHelper.ShowConfirmationAsync(
                this.XamlRoot,
                Constants.DialogTitles.CancelRequestConfirmation,
                "Are you sure you want to cancel this request?",
                Constants.DialogButtons.CancelRequest,
                Constants.DialogButtons.GoBack,
                ContentDialogButton.Close);

            if (result == ContentDialogResult.Primary)
            {
                var requestsToOthersViewModel = DataContext as RequestsToOthersViewModel;
                requestsToOthersViewModel?.CancelRequest(requestIdentifier);
            }
        }

        private void CreateRequestButton_Click(object sender, RoutedEventArgs e)
        {
            Frame?.Navigate(typeof(CreateRequestView));
        }

        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (sender is not Image failedImage) return;

            if (failedImage.Source is BitmapImage current &&
                current.UriSource != null &&
                current.UriSource.AbsoluteUri.EndsWith("/Assets/default-game-placeholder.jpg", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (Resources.TryGetValue("DefaultGameImage", out var localResource) && localResource is BitmapImage localImage)
            {
                failedImage.Source = localImage;
                return;
            }

            if (Application.Current.Resources.TryGetValue("DefaultGameImage", out var appResource) && appResource is BitmapImage appImage)
            {
                failedImage.Source = appImage;
                return;
            }

            failedImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/default-game-placeholder.jpg"));
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

