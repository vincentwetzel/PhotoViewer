using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace PhotoViewer.ViewModels
{
    public class PhotoThumbnailViewModel : INotifyPropertyChanged
    {
        private BitmapImage? _thumbnail;

        public string FilePath { get; }

        public BitmapImage? Thumbnail
        {
            get => _thumbnail;
            set { _thumbnail = value; OnPropertyChanged(); }
        }

        public PhotoThumbnailViewModel(string filePath)
        {
            FilePath = filePath;
        }

        public async Task LoadThumbnailAsync()
        {
            if (!File.Exists(FilePath)) return;

            // Run the file I/O and image decoding on a background thread
            var thumbnail = await Task.Run(() =>
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(FilePath);
                image.DecodePixelWidth = 150; // Create a smaller image in memory
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                
                // Freeze the image to make it accessible across threads
                image.Freeze();
                
                return image;
            });

            // The awaiter returns to the UI thread, so this assignment is safe
            Thumbnail = thumbnail;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}