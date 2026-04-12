using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Viewmodels;

namespace Property_and_Management.Src.Views
{
    public sealed partial class ListingsPage : Page
    {
        private const int NoActiveRentalsCount = 0;

        public ListingsViewModel ViewModel { get; private set; }

        public ListingsPage()
        {
            this.InitializeComponent();
        }

        // UI-LST-05: Redirect to Create Game page
        private void CreateGameButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(CreateGameView));
        }

        // UI-LST-04: Redirect to Edit Game page
        private void EditGameButton_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as Button;
            var gameToEdit = clickedButton?.Tag as GameDataTransferObject;

            if (gameToEdit != null)
            {
                this.Frame.Navigate(typeof(EditGameView), gameToEdit.Identifier);
            }
        }

        // UI-LST-03: Prompt for confirmation, then delete
        private async void DeleteGameButton_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as Button;
            var gameToDelete = clickedButton?.Tag as GameDataTransferObject;

            if (gameToDelete == null)
            {
                return;
            }

            var result = await DialogHelper.ShowConfirmationAsync(
                this.XamlRoot,
                Constants.DialogTitles.DeleteGameConfirmation,
                $"Are you sure you want to permanently delete '{gameToDelete.Name}'? Pending requests will be cancelled and notified. Deletion is blocked if active or upcoming rentals exist.",
                Constants.DialogButtons.Delete,
                Constants.DialogButtons.Cancel,
                ContentDialogButton.Close);

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    // Execute deletion in the ViewModel
                    ViewModel.DeleteGame(gameToDelete);

                    await DialogHelper.ShowMessageAsync(
                        this.XamlRoot,
                        Constants.DialogTitles.GameRemoved,
                        $"There are {NoActiveRentalsCount} active rentals for this game. It was removed successfully.");
                }
                catch (InvalidOperationException invalidOperationException)
                {
                    await DialogHelper.ShowMessageAsync(
                        this.XamlRoot,
                        Constants.DialogTitles.CannotDeleteGame,
                        invalidOperationException.Message);
                }
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Composition root: pull the ViewModel from the DI container
            // whenever the page is navigated to so data is fresh.
            ViewModel = App.Services.GetRequiredService<ListingsViewModel>();
            this.DataContext = ViewModel;
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.PrevPage();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.NextPage();
        }

        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            ImageFailureHandler.HandleFailure(sender as Image, Resources);
        }
    }
}

