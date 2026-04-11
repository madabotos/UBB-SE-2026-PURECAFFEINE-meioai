using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Viewmodels;

namespace Property_and_Management.src.Views
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
            var gameToEdit = clickedButton?.Tag as GameDTO;

            if (gameToEdit != null)
            {
                this.Frame.Navigate(typeof(EditGameView), gameToEdit.Id);
            }
        }

        // UI-LST-03: Prompt for confirmation, then delete
        private async void DeleteGameButton_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as Button;
            var gameToDelete = clickedButton?.Tag as GameDTO;

            if (gameToDelete == null) return;

            // Create the confirmation prompt
            ContentDialog deleteDialog = new ContentDialog
            {
                Title = Constants.DialogTitles.DeleteGameConfirmation,
                Content = $"Are you sure you want to permanently delete '{gameToDelete.Name}'? Pending requests will be cancelled and notified. Deletion is blocked if active or upcoming rentals exist.",
                PrimaryButtonText = Constants.DialogButtons.Delete,
                CloseButtonText = Constants.DialogButtons.Cancel,
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await deleteDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    // Execute deletion in the ViewModel
                    ViewModel.DeleteGame(gameToDelete);

                    var successDialog = new ContentDialog
                    {
                        Title = Constants.DialogTitles.GameRemoved,
                        Content = $"There are {NoActiveRentalsCount} active rentals for this game. It was removed successfully.",
                        CloseButtonText = Constants.DialogButtons.Ok,
                        XamlRoot = this.XamlRoot
                    };
                    await successDialog.ShowAsync();
                }
                catch (InvalidOperationException invalidOperationException)
                {
                    var blockedDialog = new ContentDialog
                    {
                        Title = Constants.DialogTitles.CannotDeleteGame,
                        Content = invalidOperationException.Message,
                        CloseButtonText = Constants.DialogButtons.Ok,
                        XamlRoot = this.XamlRoot
                    };
                    await blockedDialog.ShowAsync();
                }
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

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
            if (sender is not Image failedImage)
            {
                return;
            }

            if (failedImage.Source is BitmapImage current &&
                current.UriSource != null &&
                current.UriSource.AbsoluteUri.EndsWith("/Assets/default-game-placeholder.jpg", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (Resources.TryGetValue("DefaultGameImage", out var localResource) && localResource is BitmapImage localImage)
            {
                failedImage.Source = localImage;
                return;
            }

            if (Application.Current.Resources.TryGetValue("DefaultGameImage", out var appResource) && appResource is BitmapImage appImage)
            {
                failedImage.Source = appImage;
                return;
            }

            failedImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/default-game-placeholder.jpg"));
        }
    }
}
