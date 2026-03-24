using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Property_and_Management.src.Viewmodels
{
    public class MenuBarViewModel : INotifyPropertyChanged
    {
        // The View will listen to this event to know when to switch pages
        public event Action<Type> RequestNavigation;

        private string _selectedPageName;

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
            switch (pageName)
            {
                case "Listings":
                    RequestNavigation?.Invoke(typeof(Views.ListingsPage));
                    break;
                case "Notifications":
                    RequestNavigation?.Invoke(typeof(Views.NotificationsPage));
                    break;
                    // Add the rest of your cases here
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
