using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Property_and_Management;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Viewmodels;

namespace Property_and_Management.Src.Views
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
            ViewModel.SelectedGame = GamePicker.SelectedItem as GameDataTransferObject;
        }

        private void RenterPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectedRenter = RenterPicker.SelectedItem as UserDataTransferObject;
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
                    {
                        Frame.GoBack();
                    }
                }
                else
                {
                    await DialogHelper.ShowMessageAsync(this.XamlRoot, Constants.DialogTitles.RentalFailed, error);
                }
            }
            else
            {
                await DialogHelper.ShowMessageAsync(
                    this.XamlRoot,
                    Constants.DialogTitles.ValidationError,
                    Constants.DialogMessages.CreateRentalValidationError);
            }
        }
    }
}
