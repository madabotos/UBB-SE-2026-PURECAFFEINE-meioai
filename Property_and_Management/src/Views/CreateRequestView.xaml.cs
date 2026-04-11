using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Property_and_Management;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Service;
using Property_and_Management.src.Viewmodels;

namespace Property_and_Management.src.Views
{
    public sealed partial class CreateRequestView : Page
    {
        private const int MinimumSuccessfulEntityId = 1;

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
                if (result >= MinimumSuccessfulEntityId)
                {
                    if (Frame.CanGoBack)
                        Frame.GoBack();
                }
                else
                {
                    string message = ((CreateRequestError)result) switch
                    {
                        CreateRequestError.OWNER_CANNOT_RENT_ERROR => "You cannot rent your own game.",
                        CreateRequestError.DATES_UNAVAILABLE_ERROR => "The selected dates are not available.",
                        CreateRequestError.GAMEID_DOES_NOT_EXIST_ERROR => "The selected game no longer exists.",
                        _ => Constants.DialogMessages.UnexpectedErrorOccurred
                    };
                    var dialog = new ContentDialog
                    {
                        Title = Constants.DialogTitles.RequestFailed,
                        Content = message,
                        CloseButtonText = Constants.DialogButtons.Ok,
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
            }
            else
            {
                var dialog = new ContentDialog
                {
                    Title = Constants.DialogTitles.ValidationError,
                    Content = Constants.DialogMessages.CreateRequestValidationError,
                    CloseButtonText = Constants.DialogButtons.Ok,
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
    }
}
