using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Property_and_Management.Src.Viewmodels;

namespace Property_and_Management.Src.Views
{
    public sealed partial class CreateGameView : Page
    {
        public CreateGameViewModel ViewModel { get; }

        public CreateGameView()
        {
            this.InitializeComponent();

            // Composition root: pull the view model from the DI container. This
            // is the only place the view knows about <c>App.Services</c>.
            ViewModel = App.Services.GetRequiredService<CreateGameViewModel>();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SetPriceFromText(PriceNumberBox.Text);

            var submitResult = ViewModel.SubmitCreateGame();
            if (submitResult.IsSuccess)
            {
                if (Frame.CanGoBack)
                {
                    Frame.GoBack();
                }
                return;
            }

            await DialogHelper.ShowMessageAsync(
                this.XamlRoot,
                submitResult.DialogTitle,
                submitResult.DialogMessage);
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

            // WinUI 3 quirk: the picker has to be explicitly told which window it belongs to.
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file == null)
            {
                return;
            }

            FileNameTextBlock.Text = file.Name;

            using (var stream = await file.OpenStreamForReadAsync())
            {
                using (var memoryStream = new System.IO.MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);
                    ViewModel.Image = memoryStream.ToArray();
                }
            }

            var bitmapImage = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
            using (var randomAccessStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
            {
                await bitmapImage.SetSourceAsync(randomAccessStream);
            }
            ImagePreview.Source = bitmapImage;
        }
    }
}
