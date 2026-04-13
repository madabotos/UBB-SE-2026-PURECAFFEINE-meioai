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

        private void GamePicker_SelectionChanged(object sender, SelectionChangedEventArgs selectionChangedEventArgs)
        {
            ViewModel.SelectedGame = GamePicker.SelectedItem as GameDataTransferObject;
        }

        private void RenterPicker_SelectionChanged(object sender, SelectionChangedEventArgs selectionChangedEventArgs)
        {
            ViewModel.SelectedRenter = RenterPicker.SelectedItem as UserDataTransferObject;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            ViewModel.StartDate = StartDatePicker.Date;
            ViewModel.EndDate = EndDatePicker.Date;

            var createResult = ViewModel.CreateRental();
            if (createResult.IsSuccess)
            {
                if (Frame.CanGoBack)
                {
                    Frame.GoBack();
                }
                return;
            }

            await DialogHelper.ShowMessageAsync(
                this.XamlRoot,
                createResult.DialogTitle,
                createResult.DialogMessage);
        }
    }
}
