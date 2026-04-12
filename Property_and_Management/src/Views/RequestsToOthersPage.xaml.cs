using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management.Src.Viewmodels;

namespace Property_and_Management.Src.Views
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
                // Composition root: fall back to the DI container when no
                // navigation parameter was passed.
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
            {
                return;
            }

            var result = await DialogHelper.ShowConfirmationAsync(
                this.XamlRoot,
                Constants.DialogTitles.CancelRequestConfirmation,
                "Are you sure you want to cancel this request?",
                Constants.DialogButtons.CancelRequest,
                Constants.DialogButtons.GoBack,
                ContentDialogButton.Close);

            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            var requestsToOthersViewModel = DataContext as RequestsToOthersViewModel;
            var error = requestsToOthersViewModel?.TryCancelRequest(requestIdentifier);
            if (error != null)
            {
                await DialogHelper.ShowMessageAsync(
                    this.XamlRoot,
                    Constants.DialogTitles.CancelRequestConfirmation,
                    error);
            }
        }

        private void CreateRequestButton_Click(object sender, RoutedEventArgs e)
        {
            Frame?.Navigate(typeof(CreateRequestView));
        }

        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            ImageFailureHandler.HandleFailure(sender as Image, Resources);
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
