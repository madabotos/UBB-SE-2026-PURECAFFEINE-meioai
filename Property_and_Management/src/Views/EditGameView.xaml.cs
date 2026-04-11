using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management;
using Property_and_Management.src.Viewmodels;
using Windows.Foundation;
using Windows.Foundation.Collections;


namespace Property_and_Management.src.Views
{
    public sealed partial class EditGameView : Page
    {
        private const int EmptyImageLength = 0;
        private const decimal InvalidOrEmptyPriceValue = 0m;

        public EditGameViewModel ViewModel { get; }

        public EditGameView()
        {
            this.InitializeComponent();
            ViewModel = App.Services.GetRequiredService<EditGameViewModel>();
        }

        // Catches the Game ID passed from the Listings Page
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // If the navigation parameter is an integer, load that game's data 
            if (e.Parameter is int incomingGameId)
            {
                ViewModel.LoadGame(incomingGameId);
                this.Bindings.Update();
            }

            if (ViewModel.Image != null && ViewModel.Image.Length > EmptyImageLength)
            {
                using (var memoryStream = new System.IO.MemoryStream(ViewModel.Image))
                {
                    // Convert the database bytes into a stream the Image control can read
                    var randomAccessStream = memoryStream.AsRandomAccessStream();
                    var bitmapImage = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                    await bitmapImage.SetSourceAsync(randomAccessStream);

                    // Put the picture into the little preview box!
                    ImagePreview.Source = bitmapImage;
                }
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SyncPriceFromInput();
            var validationErrors = ViewModel.ValidateInputs();

            // Validate modified inputs [cite: 132]
            if (!validationErrors.Any())
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
                    Title = Constants.DialogTitles.ValidationError,
                    Content = string.Join(Environment.NewLine, validationErrors),
                    CloseButtonText = Constants.DialogButtons.Ok,
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        private void SyncPriceFromInput()
        {
            var priceText = PriceNumberBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(priceText))
            {
                ViewModel.Price = InvalidOrEmptyPriceValue;
                return;
            }

            if (double.TryParse(priceText, NumberStyles.Float, CultureInfo.CurrentCulture, out var parsedPrice) ||
                double.TryParse(priceText, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedPrice))
            {
                ViewModel.PriceDouble = parsedPrice;
                return;
            }

            ViewModel.Price = InvalidOrEmptyPriceValue;
        }

        private async void UploadImageButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Set up the Windows File Picker
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            // WinUI 3 Quirk: We have to explicitly tell the picker which window it belongs to
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            // 2. Open the picker and wait for the user to select a file
            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                FileNameTextBlock.Text = file.Name;

                // 3. Convert the image file into a byte array for the Database
                using (var stream = await file.OpenStreamForReadAsync())
                {
                    using (var memoryStream = new System.IO.MemoryStream())
                    {
                        await stream.CopyToAsync(memoryStream);

                        // Save the bytes to your ViewModel!
                        ViewModel.Image = memoryStream.ToArray();
                    }
                }

                // 4. Update the little UI preview box so the user sees what they picked
                var bitmapImage = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                using (var irandomAccessStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    await bitmapImage.SetSourceAsync(irandomAccessStream);
                }
                ImagePreview.Source = bitmapImage;
            }
        }
    }
}
