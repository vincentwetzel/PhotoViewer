using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace PhotoViewer.Converters
{
    public class ImagePathToThumbnailConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string imagePath || string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                return null;
            }

            try
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(imagePath);
                bitmapImage.DecodePixelWidth = 500; // Larger thumbnail for better quality when scaled
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // Freeze for performance benefits on background threads
                return bitmapImage;
            }
            catch
            {
                // Return null if the image is corrupt or cannot be loaded.
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
