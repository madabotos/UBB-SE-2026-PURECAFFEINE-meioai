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
            ViewModel = App.Services.GetRequiredService<CreateGameViewModel>();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SyncPriceFromInput();
            var validationErrors = ViewModel.ValidateInputs();

            // Validate inputs before saving [cite: 123]
            if (!validationErrors.Any())
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
                    Content = string.Join(Environment.NewLine, validationErrors),
                    CloseButtonText = "OK",
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
                ViewModel.Price = 0;
                return;
            }

            if (double.TryParse(priceText, NumberStyles.Float, CultureInfo.CurrentCulture, out var parsedPrice) ||
                double.TryParse(priceText, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedPrice))
            {
                ViewModel.PriceDouble = parsedPrice;
                return;
            }

            ViewModel.Price = 0;
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
