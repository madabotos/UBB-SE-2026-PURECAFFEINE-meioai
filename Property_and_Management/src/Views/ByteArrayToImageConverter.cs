using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;

namespace Property_and_Management.src.Converters
{
    public class ByteArrayToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is byte[] bytes && bytes.Length > 0)
            {
                using (var ms = new MemoryStream(bytes))
                {
                    var image = new BitmapImage();
                    // SetSourceAsync is required in WinUI to load streams
                    image.SetSource(ms.AsRandomAccessStream());
                    return image;
                }
            }

            // UI-LST-02: Default placeholder if none is uploaded
            return new BitmapImage(new Uri("ms-appx:///Assets/PlaceholderGame.png"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
