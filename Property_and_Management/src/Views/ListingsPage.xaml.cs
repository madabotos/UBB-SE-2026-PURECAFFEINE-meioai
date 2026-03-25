using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Service;
using Property_and_Management.src.Viewmodels;

namespace Property_and_Management.src.Views
{
    public sealed partial class ListingsPage : Page
    {
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
            var btn = sender as Button;
            var gameToEdit = btn?.Tag as GameDTO;

            if (gameToEdit != null)
            {
                this.Frame.Navigate(typeof(EditGameView), gameToEdit.Id);
            }
        }

        // UI-LST-03: Prompt for confirmation, then delete
        private async void DeleteGameButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var gameToDelete = btn?.Tag as GameDTO;

            if (gameToDelete == null) return;

            // Create the confirmation prompt
            ContentDialog deleteDialog = new ContentDialog
            {
                Title = "Delete Game?",
                Content = $"Are you sure you want to permanently delete '{gameToDelete.Name}' and all associated active requests? Existing rentals will not be deleted.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await deleteDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Execute deletion in the ViewModel
                ViewModel.DeleteGame(gameToDelete);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is GameService gameService)
            {
                // We still need the current user ID. 
                var app = (Property_and_Management.App)Application.Current;

                ViewModel = new ListingsViewModel(gameService, app.CurrentUserID);
                this.DataContext = ViewModel;
            }
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.PrevPage();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.NextPage();
        }
    }
}
