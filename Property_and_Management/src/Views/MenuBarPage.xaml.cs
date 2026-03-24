using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation; // Need this for OnNavigatedTo
using Property_and_Management.src.Interface;
using Property_and_Management.src.Viewmodels;

namespace Property_and_Management.src.Views
{
    public sealed partial class MenuBarView : Page
    {
        public MenuBarViewModel ViewModel { get; }

        // The Menu stores a private copy of the service to pass out later
        private IGameService _passedGameService;

        public MenuBarView()
        {
            this.InitializeComponent();
            ViewModel = new MenuBarViewModel();
            this.DataContext = ViewModel;
            ViewModel.RequestNavigation += OnViewModelRequestedNavigation;


        }

        // Catch the service that App.xaml.cs threw to us
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is IGameService gameService)
            {
                _passedGameService = gameService;
            }
        }

        // When the user clicks "Listings", pass the service to the new page!
        private void OnViewModelRequestedNavigation(System.Type pageType)
        {
            // Pass the service right through the ContentFrame!
            ContentFrame.Navigate(pageType, _passedGameService);
        }

        public void NavigateToNotifications()
        {
            var app = Application.Current as Property_and_Management.App;
            ContentFrame.Navigate(typeof(NotificationsPage), app?.NotificationsViewModel);
            ViewModel.SelectedPageName = "Notifications";
        }
    }
}
