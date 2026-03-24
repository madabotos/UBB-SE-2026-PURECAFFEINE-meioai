using Microsoft.UI.Xaml.Controls;
using Property_and_Management.src.Viewmodels;

namespace Property_and_Management.src.Views
{
    public sealed partial class MenuBarView : Page
    {
        public MenuBarViewModel ViewModel { get; }

        public MenuBarView()
        {
            this.InitializeComponent();

            // 1. Create the ViewModel and set it as the DataContext for bindings
            ViewModel = new MenuBarViewModel();
            this.DataContext = ViewModel;

            // 2. Listen for the ViewModel asking to navigate
            ViewModel.RequestNavigation += OnViewModelRequestedNavigation;
        }

        // 3. When the ViewModel says "Go", the View physically moves the Frame
        private void OnViewModelRequestedNavigation(System.Type pageType)
        {
            ContentFrame.Navigate(pageType);
        }
    }
}
