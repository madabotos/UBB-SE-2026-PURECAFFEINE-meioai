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

        private void DenyButton_Tapped(object sender, TappedRoutedEventArgs e)
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
                Title = "Approve Request?",
                Content = $"Approve request for {gameName} from {renterName} for {request?.StartDateDisplayLong} - {request?.EndDateDisplayLong}? A rental will be created immediately.",
                PrimaryButtonText = "Approve",
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
                        -3 => "This request is no longer open.",
                        _ => "An unexpected error occurred."
                    };
                    var errorDialog = new ContentDialog
                    {
                        Title = "Approve Failed",
                        Content = message,
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }

        private async void DenyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not int requestId)
                return;

            var request = btn.DataContext as RequestDTO;
            var gameName = request?.Game?.Name ?? "this game";
            var renterName = request?.Renter?.DisplayName ?? "the requester";

            var reasonBox = new TextBox
            {
                PlaceholderText = "Optional reason (e.g. unavailable in this period)",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinWidth = 360
            };

            var contentPanel = new StackPanel { Spacing = 8 };
            contentPanel.Children.Add(new TextBlock
            {
                Text = $"Decline request for {gameName} from {renterName}?"
            });
            contentPanel.Children.Add(reasonBox);

            var denyDialog = new ContentDialog
            {
                Title = "Decline Request?",
                Content = contentPanel,
                PrimaryButtonText = "Decline",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var dialogResult = await denyDialog.ShowAsync();
            if (dialogResult != ContentDialogResult.Primary)
                return;

            var reason = (reasonBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(reason))
            {
                reason = "No reason provided.";
            }

            var vm = DataContext as RequestsFromOthersViewModel;
            var denyResult = vm?.DenyRequest(requestId, reason) ?? -1;

            if (denyResult < 0)
            {
                string message = denyResult switch
                {
                    -1 => "Request not found.",
                    -2 => "You are not authorized to deny this request.",
                    _ => "An unexpected error occurred."
                };

                var errorDialog = new ContentDialog
                {
                    Title = "Decline Failed",
                    Content = message,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
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
