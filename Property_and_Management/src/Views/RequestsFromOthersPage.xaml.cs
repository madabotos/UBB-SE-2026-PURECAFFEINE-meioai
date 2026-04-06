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
    public sealed partial class RequestsFromOthersPage : Page
    {
        public RequestsFromOthersPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is RequestsFromOthersViewModel vm)
            {
                DataContext = vm;
                return;
            }

            if (DataContext is not RequestsFromOthersViewModel)
            {
                DataContext = App.Services.GetRequiredService<RequestsFromOthersViewModel>();
            }
        }

        private void OfferButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private async void OfferButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not int requestId)
                return;

            var request = btn.DataContext as RequestDTO;
            var gameName = request?.Game?.Name ?? "this game";
            var renterName = request?.Renter?.DisplayName ?? "the requester";

            ContentDialog offerDialog = new ContentDialog
            {
                Title = "Offer Game?",
                Content = $"Offer {gameName} to {renterName} for {request?.StartDateDisplayLong} - {request?.EndDateDisplayLong}?",
                PrimaryButtonText = "Offer",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await offerDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var vm = DataContext as RequestsFromOthersViewModel;
                var offerResult = vm?.OfferGame(requestId) ?? -1;

                if (offerResult < 0)
                {
                    string message = offerResult switch
                    {
                        -1 => "Request not found.",
                        -2 => "You are not the owner of this game.",
                        -3 => "This request already has a pending offer.",
                        _ => "An unexpected error occurred."
                    };
                    var errorDialog = new ContentDialog
                    {
                        Title = "Offer Failed",
                        Content = message,
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }

        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (sender is not Image img)
            {
                return;
            }

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
            var vm = DataContext as RequestsFromOthersViewModel;
            vm?.NextPage();
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as RequestsFromOthersViewModel;
            vm?.PrevPage();
        }
    }
}
