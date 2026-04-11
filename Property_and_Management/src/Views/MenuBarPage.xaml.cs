using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Viewmodels;

namespace Property_and_Management.src.Views
{
    public sealed partial class MenuBarView : Page
    {
        public MenuBarViewModel ViewModel { get; }

        // Maps page keys emitted by the ViewModel to concrete View types.
        // Concrete type knowledge belongs in the View layer, not in ViewModels.
        private static readonly Dictionary<AppPage, Type> PageTypeMap = new()
        {
            { AppPage.Listings,            typeof(ListingsPage) },
            { AppPage.RequestsFromOthers,  typeof(RequestsFromOthersPage) },
            { AppPage.RentalsFromOthers,   typeof(RentalsFromOthersPage) },
            { AppPage.RequestsToOthers,    typeof(RequestsToOthersPage) },
            { AppPage.RentalsToOthers,     typeof(RentalsToOthersPage) },
            { AppPage.Notifications,       typeof(NotificationsPage) }
        };

        // The Menu keeps a reference to IGameService so it can pass it to pages that need it.
        private IGameService _passedGameService;

        public MenuBarView()
        {
            this.InitializeComponent();
            ViewModel = App.Services.GetRequiredService<MenuBarViewModel>();
            this.DataContext = ViewModel;
            ViewModel.RequestNavigation += OnViewModelRequestedNavigation;
            this.Unloaded += (pageSender, unloadedEventArgs) => ViewModel.RequestNavigation -= OnViewModelRequestedNavigation;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is IGameService gameService)
                _passedGameService = gameService;
        }

        private void OnViewModelRequestedNavigation(AppPage page)
        {
            if (!PageTypeMap.TryGetValue(page, out var pageType))
                return;

            ContentFrame.Navigate(pageType, _passedGameService);
        }

        public void NavigateToNotifications()
        {
            var applicationInstance = Application.Current as Property_and_Management.App;
            ContentFrame.Navigate(typeof(NotificationsPage), applicationInstance?.NotificationsViewModel);
            ViewModel.SelectedPageName = "Notifications";
        }
    }
}
