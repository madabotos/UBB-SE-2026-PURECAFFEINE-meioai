using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Viewmodels;

namespace Property_and_Management.src.Views
{
    public sealed partial class CreateRequestView : Page
    {
        public CreateRequestViewModel ViewModel { get; }

        public CreateRequestView()
        {
            ViewModel = App.Services.GetRequiredService<CreateRequestViewModel>();
            this.InitializeComponent();

            GamePicker.ItemsSource = ViewModel.AvailableGames;
            StartDatePicker.MinDate = DateTimeOffset.Now;
            EndDatePicker.MinDate = DateTimeOffset.Now;
        }

        private void GamePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectedGame = GamePicker.SelectedItem as GameDTO;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartDate = StartDatePicker.Date;
            ViewModel.EndDate = EndDatePicker.Date;

            if (ViewModel.ValidateInputs())
            {
                var result = ViewModel.SaveRequest();
                if (result > 0)
                {
                    if (Frame.CanGoBack)
                        Frame.GoBack();
                }
                else
                {
                    string message = result switch
                    {
                        -1 => "You cannot rent your own game.",
                        -2 => "The selected dates are not available.",
                        -3 => "The selected game no longer exists.",
                        _ => "An unexpected error occurred."
                    };
                    var dialog = new ContentDialog
                    {
                        Title = "Request Failed",
                        Content = message,
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
                    Content = "Please select a game and valid date range (start date must be before end date and not in the past).",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
    }
}
