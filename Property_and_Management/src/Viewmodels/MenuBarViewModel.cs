using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Property_and_Management.src.Views;

namespace Property_and_Management.src.Viewmodels
{
    public class MenuBarViewModel : INotifyPropertyChanged
    {
        // The View will listen to this event to know when to switch pages
        public event Action<Type> RequestNavigation;

        public Dictionary<string, Action> NavigationActions { get; }

        private string _selectedPageName;

        public MenuBarViewModel()
        {
            NavigationActions = new Dictionary<string, Action>
            {
                { "Listings", () => RequestNavigation?.Invoke(typeof(ListingsPage)) },
                { "Others' Requests", () => RequestNavigation?.Invoke(typeof(RequestsFromOthersPage)) },
                { "Others' Rentals", () => throw new NotImplementedException("Others' Rentals navigation is not yet implemented.") },
                { "My Requests", () => RequestNavigation?.Invoke(typeof(RequestsToOthersPage)) },
                { "My Rentals", () => throw new NotImplementedException("My Rentals navigation is not yet implemented.") },
                { "Notifications", () => RequestNavigation?.Invoke(typeof(Views.NotificationsPage)) }
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
                    OnPropertyChanged(); // Update the UI

                    // Trigger the navigation logic whenever the property changes
                    HandleNavigation(value);
                }
            }
        }

        private void HandleNavigation(string pageName)
        {
            OnPropertyChanged();

            // Execute the lambda from the dictionary
            if (!string.IsNullOrEmpty(pageName) && NavigationActions.TryGetValue(pageName, out var action))
            {
                action.Invoke();
            }
        }

        // Standard INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
