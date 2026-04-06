using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Viewmodels;

namespace Property_and_Management.src.Views
{
    public sealed partial class CreateRentalView : Page
    {
        public CreateRentalViewModel ViewModel { get; }

        public CreateRentalView()
        {
            ViewModel = App.Services.GetRequiredService<CreateRentalViewModel>();
            this.InitializeComponent();

            GamePicker.ItemsSource = ViewModel.MyGames;
            RenterPicker.ItemsSource = ViewModel.AvailableRenters;
            StartDatePicker.MinDate = DateTimeOffset.Now;
            EndDatePicker.MinDate = DateTimeOffset.Now;
        }

        private void GamePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectedGame = GamePicker.SelectedItem as GameDTO;
        }

        private void RenterPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectedRenter = RenterPicker.SelectedItem as UserDTO;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartDate = StartDatePicker.Date;
            ViewModel.EndDate = EndDatePicker.Date;

            if (ViewModel.ValidateInputs())
            {
                var error = ViewModel.SaveRental();
                if (error == null)
                {
                    if (Frame.CanGoBack)
                        Frame.GoBack();
                }
                else
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Rental Failed",
                        Content = error,
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
            }
            else
            {
                var dialog = new ContentDialog
                {
                    Title = "Validation Error",
                    Content = "Please select a game, a renter, and a valid date range (start before end, not in the past).",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
    }
}
