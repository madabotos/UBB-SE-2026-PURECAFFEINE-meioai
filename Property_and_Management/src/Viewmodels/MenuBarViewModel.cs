using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Property_and_Management.src.Viewmodels
{
    public class MenuBarViewModel : INotifyPropertyChanged
    {
        // The View listens to this event to know which page to navigate to.
        // Using AppPage enum keeps the ViewModel free of concrete View type references.
        public event Action<AppPage> RequestNavigation;

        public Dictionary<string, Action> NavigationActions { get; }

        private string _selectedPageName;

        public MenuBarViewModel()
        {
            NavigationActions = new Dictionary<string, Action>
            {
                { "My Games",           () => RequestNavigation?.Invoke(AppPage.Listings) },
                { "Others' Requests",   () => RequestNavigation?.Invoke(AppPage.RequestsFromOthers) },
                { "Others' Rentals",    () => RequestNavigation?.Invoke(AppPage.RentalsFromOthers) },
                { "My Requests",        () => RequestNavigation?.Invoke(AppPage.RequestsToOthers) },
                { "My Rentals",         () => RequestNavigation?.Invoke(AppPage.RentalsToOthers) },
                { "Notifications",      () => RequestNavigation?.Invoke(AppPage.Notifications) }
            };
        }

        public string SelectedPageName
        {
            get => _selectedPageName;
            set
            {
                if (_selectedPageName != value)
                {
                    _selectedPageName = value;
                    OnPropertyChanged();
                    HandleNavigation(value);
                }
            }
        }

        private void HandleNavigation(string pageName)
        {
            OnPropertyChanged();
            if (!string.IsNullOrEmpty(pageName) && NavigationActions.TryGetValue(pageName, out var action))
                action.Invoke();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
