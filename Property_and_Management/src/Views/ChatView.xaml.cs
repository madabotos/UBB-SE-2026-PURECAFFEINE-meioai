using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management.src.Service;
using Property_and_Management.src.Viewmodels;
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
    public sealed partial class ChatView : Page
    {
        public ChatViewModel ViewModel { get; }

        public ChatView()
        {
            this.InitializeComponent();
            ViewModel = new ChatViewModel();
        }

        // This runs the moment the page is opened!
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Catch the RequestId that was passed from the previous page
            if (e.Parameter is int incomingRequestId)
            {
                ViewModel.RequestId = incomingRequestId;
            }
        }

        private async void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            int result;
            try
            {
                result = ViewModel.Approve();
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync($"Approve failed: {ex.Message}");
                return;
            }

            if (result <= 0)
            {
                await ShowErrorDialogAsync(GetApproveErrorMessage(result));
                return;
            }

            // [MOCK-UI-CHT-03] Immediately close the Chat Page and redirect back
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private async void DenyButton_Click(object sender, RoutedEventArgs e)
        {
            int result;
            try
            {
                result = ViewModel.Deny();
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync($"Deny failed: {ex.Message}");
                return;
            }

            if (result <= 0)
            {
                await ShowErrorDialogAsync("Deny failed. The request may no longer exist or you may not be authorized.");
                return;
            }

            // [MOCK-UI-CHT-03] Immediately close the Chat Page and redirect back
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private async System.Threading.Tasks.Task ShowErrorDialogAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Operation failed",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private static string GetApproveErrorMessage(int result)
        {
            return result switch
            {
                (int)ApproveRequestError.UNAUTHORIZED_ERROR => "Approve failed: you are not authorized for this request.",
                (int)ApproveRequestError.NOT_FOUND_ERROR => "Approve failed: request was not found.",
                (int)ApproveRequestError.TRANSACTION_FAILED_ERROR => "Approve failed due to a database transaction error.",
                _ => "Approve failed due to an unexpected error."
            };
        }
    }
}
