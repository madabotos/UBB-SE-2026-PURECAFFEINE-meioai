using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management.src.Viewmodels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management.src.Viewmodels;


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

        private void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            // Call the ViewModel method to do the database work
            ViewModel.Approve();

            // [MOCK-UI-CHT-03] Immediately close the Chat Page and redirect back
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private void DenyButton_Click(object sender, RoutedEventArgs e)
        {
            // Call the ViewModel method to do the database work
            ViewModel.Deny();

            // [MOCK-UI-CHT-03] Immediately close the Chat Page and redirect back
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }
    }
}
