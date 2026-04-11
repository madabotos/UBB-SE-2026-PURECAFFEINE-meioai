using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management;
using Property_and_Management.src.DataTransferObjects;
using Property_and_Management.src.Service;
using Property_and_Management.src.Viewmodels;

namespace Property_and_Management.src.Views
{
    public sealed partial class RequestsFromOthersPage : Page
    {
        private const double DenyReasonInputMinimumWidth = 360;
        private const double DenyDialogContentSpacing = 8;
        private const int UnknownOperationResult = -1;
        private const int MinimumSuccessfulEntityId = 1;
        private const int ErrorResultUpperBoundExclusive = 0;

        public RequestsFromOthersPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is RequestsFromOthersViewModel requestsFromOthersViewModel)
            {
                DataContext = requestsFromOthersViewModel;
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
            if (sender is not Button clickedButton || clickedButton.Tag is not int requestId)
                return;

            var request = clickedButton.DataContext as RequestDataTransferObject;
            var gameName = request?.Game?.Name ?? "this game";
            var renterName = request?.Renter?.DisplayName ?? "the requester";

            var result = await DialogHelper.ShowConfirmationAsync(
                this.XamlRoot,
                Constants.DialogTitles.ApproveRequestConfirmation,
                $"Approve request for {gameName} from {renterName} for {request?.StartDateDisplayLong} - {request?.EndDateDisplayLong}? A rental will be created immediately.",
                Constants.DialogButtons.Approve,
                Constants.DialogButtons.Cancel,
                ContentDialogButton.Primary);

            if (result == ContentDialogResult.Primary)
            {
                var requestsFromOthersViewModel = DataContext as RequestsFromOthersViewModel;
                var offerResult = requestsFromOthersViewModel?.OfferGame(requestId) ?? UnknownOperationResult;

                if (offerResult < MinimumSuccessfulEntityId)
                {
                    string message = offerResult < ErrorResultUpperBoundExclusive
                        ? ((OfferError)offerResult) switch
                        {
                            OfferError.NOT_FOUND => "Request not found.",
                            OfferError.NOT_OWNER => "You are not the owner of this game.",
                            OfferError.REQUEST_NOT_OPEN => "This request is no longer open.",
                            _ => Constants.DialogMessages.UnexpectedErrorOccurred
                        }
                        : Constants.DialogMessages.UnexpectedErrorOccurred;

                    await DialogHelper.ShowMessageAsync(this.XamlRoot, Constants.DialogTitles.ApproveFailed, message);
                }
            }
        }

        private async void DenyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button clickedButton || clickedButton.Tag is not int requestId)
                return;

            var request = clickedButton.DataContext as RequestDataTransferObject;
            var gameName = request?.Game?.Name ?? "this game";
            var renterName = request?.Renter?.DisplayName ?? "the requester";

            var reasonBox = new TextBox
            {
                PlaceholderText = "Optional reason (e.g. unavailable in this period)",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinWidth = DenyReasonInputMinimumWidth
            };

            var contentPanel = new StackPanel { Spacing = DenyDialogContentSpacing };
            contentPanel.Children.Add(new TextBlock
            {
                Text = $"Decline request for {gameName} from {renterName}?"
            });
            contentPanel.Children.Add(reasonBox);

            var dialogResult = await DialogHelper.ShowConfirmationAsync(
                this.XamlRoot,
                Constants.DialogTitles.DeclineRequestConfirmation,
                contentPanel,
                Constants.DialogButtons.Decline,
                Constants.DialogButtons.Cancel,
                ContentDialogButton.Primary);
            if (dialogResult != ContentDialogResult.Primary)
                return;

            var reason = (reasonBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(reason))
            {
                reason = Constants.DialogMessages.NoReasonProvided;
            }

            var requestsFromOthersViewModel = DataContext as RequestsFromOthersViewModel;
            var denyResult = requestsFromOthersViewModel?.DenyRequest(requestId, reason) ?? UnknownOperationResult;

            if (denyResult < MinimumSuccessfulEntityId)
            {
                string message = denyResult < ErrorResultUpperBoundExclusive
                    ? ((DenyRequestError)denyResult) switch
                    {
                        DenyRequestError.NOT_FOUND_ERROR => "Request not found.",
                        DenyRequestError.UNAUTHORIZED_ERROR => "You are not authorized to deny this request.",
                        _ => Constants.DialogMessages.UnexpectedErrorOccurred
                    }
                    : Constants.DialogMessages.UnexpectedErrorOccurred;

                await DialogHelper.ShowMessageAsync(this.XamlRoot, Constants.DialogTitles.DeclineFailed, message);
            }
        }

        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (sender is not Image failedImage)
            {
                return;
            }

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
            var requestsFromOthersViewModel = DataContext as RequestsFromOthersViewModel;
            requestsFromOthersViewModel?.NextPage();
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            var requestsFromOthersViewModel = DataContext as RequestsFromOthersViewModel;
            requestsFromOthersViewModel?.PrevPage();
        }
    }
}
