using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management.src.Viewmodels;
using Windows.Foundation;
using Windows.Foundation.Collections;


namespace Property_and_Management.src.Views
{
    public sealed partial class EditGameView : Page
    {
        public EditGameViewModel ViewModel { get; }

        public EditGameView()
        {
            this.InitializeComponent();
            ViewModel = new EditGameViewModel();
        }

        // Catches the Game ID passed from the Listings Page
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // If the navigation parameter is an integer, load that game's data 
            if (e.Parameter is int incomingGameId)
            {
                ViewModel.LoadGame(incomingGameId);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate modified inputs [cite: 132]
            if (ViewModel.ValidateInputs())
            {
                ViewModel.UpdateGame();

                // On successful validation, redirect back to Listings page [cite: 134]
                if (Frame.CanGoBack)
                {
                    Frame.GoBack();
                }
            }
            else
            {
                // If validation fails, display inline error [cite: 133]
                var dialog = new ContentDialog
                {
                    Title = "Validation Error",
                    Content = "Please ensure all fields are filled out correctly according to the rules.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
    }
}
