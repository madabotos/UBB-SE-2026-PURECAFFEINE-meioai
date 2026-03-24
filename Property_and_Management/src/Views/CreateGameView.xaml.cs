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
    public sealed partial class CreateGameView : Page
    {
        public CreateGameViewModel ViewModel { get; }

        public CreateGameView()
        {
            this.InitializeComponent();
            ViewModel = new CreateGameViewModel();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate inputs before saving [cite: 123]
            if (ViewModel.ValidateInputs())
            {
                ViewModel.SaveGame();

                // On successful validation, redirect user to the Listings page 
                if (Frame.CanGoBack)
                {
                    Frame.GoBack();
                }
            }
            else
            {
                // If validation fails, display an error message [cite: 124]
                var dialog = new ContentDialog
                {
                    Title = "Validation Error",
                    Content = "Please ensure all fields are filled out correctly according to the rules (e.g., proper name length, positive price, valid player count).",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
    }
}
