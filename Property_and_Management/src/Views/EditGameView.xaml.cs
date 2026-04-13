using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management.Src.Viewmodels;

namespace Property_and_Management.Src.Views
{
    public sealed partial class EditGameView : Page
    {
        private const int EmptyImageLength = 0;

        public EditGameViewModel ViewModel { get; }

        public EditGameView()
        {
            this.InitializeComponent();

            // Composition root: pull the view model from the DI container. This
            // is the only place the view knows about <c>App.Services</c>.
            ViewModel = App.Services.GetRequiredService<EditGameViewModel>();
        }

        // Catches the game id passed in from the Listings page.
        protected override async void OnNavigatedTo(NavigationEventArgs navigationEventArgs)
        {
            base.OnNavigatedTo(navigationEventArgs);

            if (navigationEventArgs.Parameter is int incomingGameId)
            {
                ViewModel.LoadGame(incomingGameId);
                this.Bindings.Update();
            }

            if (ViewModel.Image != null && ViewModel.Image.Length > EmptyImageLength)
            {
                using (var memoryStream = new System.IO.MemoryStream(ViewModel.Image))
                {
                    var randomAccessStream = memoryStream.AsRandomAccessStream();
                    var bitmapImage = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                    await bitmapImage.SetSourceAsync(randomAccessStream);
                    ImagePreview.Source = bitmapImage;
                }
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            ViewModel.SetPriceFromText(PriceNumberBox.Text);

            var submitResult = ViewModel.SubmitGameUpdate();
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

        private async void UploadImageButton_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            // WinUI 3 quirk: the picker has to be explicitly told which window it belongs to.
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);

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
