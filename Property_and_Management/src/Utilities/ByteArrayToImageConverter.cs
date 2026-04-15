using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Property_and_Management.Src.Utilities
{
    public class ByteArrayToImageConverter : IValueConverter
    {
        private const int EmptyByteArrayLength = 0;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is byte[] bytes && bytes.Length > EmptyByteArrayLength)
            {
                try
                {
                    var imageStream = new MemoryStream(bytes);
                    var image = new BitmapImage();
                    image.SetSource(imageStream.AsRandomAccessStream());
                    return image;
                }
                catch
                {
                }
            }

            return new BitmapImage(new Uri("ms-appx:///Assets/default-game-placeholder.jpg"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}