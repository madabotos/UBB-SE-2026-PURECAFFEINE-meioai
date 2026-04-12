using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Viewmodels;

namespace Property_and_Management.Src.Views
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
        private IGameService passedGameService;

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
            {
                passedGameService = gameService;
            }
        }

        private void OnViewModelRequestedNavigation(AppPage page)
        {
            if (!PageTypeMap.TryGetValue(page, out var pageType))
            {
                return;
            }

            ContentFrame.Navigate(pageType, passedGameService);
        }

        public void NavigateToNotifications()
        {
            // Composition root: fetch the singleton NotificationsViewModel from
            // the DI container instead of reaching into Application.Current.
            var notificationsViewModel = App.Services.GetRequiredService<NotificationsViewModel>();
            ContentFrame.Navigate(typeof(NotificationsPage), notificationsViewModel);
            ViewModel.SelectedPageName = "Notifications";
        }
    }
}
