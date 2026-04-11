using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Viewmodels;

namespace Property_and_Management.Src.Views
{
    public sealed partial class RequestsFromOthersPage : Page
    {
        private const double DenyReasonInputMinimumWidth = 360;
        private const double DenyDialogContentSpacing = 8;

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
                // Composition root: fall back to the DI container when no
                // navigation parameter was passed.
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
            if (sender is not Button clickedButton || clickedButton.Tag is not int requestIdentifier)
            {
                return;
            }

            var request = clickedButton.DataContext as RequestDataTransferObject;
            var gameName = request?.Game?.Name ?? "this game";
            var renterName = request?.Renter?.DisplayName ?? "the requester";

            var result = await DialogHelper.ShowConfirmationAsync(
                this.XamlRoot,
                Constants.DialogTitles.OfferGameConfirmation,
                $"Offer {gameName} to {renterName} for {request?.StartDateDisplayLong} - {request?.EndDateDisplayLong}? They will be notified and can approve or deny the offer.",
                Constants.DialogButtons.Offer,
                Constants.DialogButtons.Cancel,
                ContentDialogButton.Primary);

            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            var requestsFromOthersViewModel = DataContext as RequestsFromOthersViewModel;
            var error = requestsFromOthersViewModel?.TryOfferGame(requestIdentifier);
            if (error != null)
            {
                await DialogHelper.ShowMessageAsync(this.XamlRoot, Constants.DialogTitles.OfferFailed, error);
            }
        }

        private async void DenyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button clickedButton || clickedButton.Tag is not int requestIdentifier)
            {
                return;
            }

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
            {
                return;
            }

            // Raw reason is handed straight to the ViewModel; trimming and the
            // "no reason provided" fallback live there so the code-behind stays
            // UI-only.
            var requestsFromOthersViewModel = DataContext as RequestsFromOthersViewModel;
            var error = requestsFromOthersViewModel?.TryDenyRequest(requestIdentifier, reasonBox.Text);
            if (error != null)
            {
                await DialogHelper.ShowMessageAsync(this.XamlRoot, Constants.DialogTitles.DeclineFailed, error);
            }
        }

        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            ImageFailureHandler.HandleFailure(sender as Image, Resources);
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
