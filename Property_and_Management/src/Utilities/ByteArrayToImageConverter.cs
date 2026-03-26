using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Property_and_Management.src.Utilities
{
    public class ByteArrayToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is byte[] bytes && bytes.Length > 0)
            {
                try
                {
                    // Keep the stream alive for the BitmapImage lifetime to avoid intermittent
                    // decode failures when list items are recycled during paging/virtualization.
                    var ms = new MemoryStream(bytes);
                    var image = new BitmapImage();
                    image.SetSource(ms.AsRandomAccessStream());
                    return image;
                }
                catch
                {
                    // Fall through to placeholder if bytes are invalid/corrupted.
                }
            }

            // UI-LST-02: Default placeholder if none is uploaded
            return new BitmapImage(new Uri("ms-appx:///Assets/default-game-placeholder.jpg"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
