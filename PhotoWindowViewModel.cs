using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using System; // For Uri

namespace PhotoViewer.ViewModels
{
    public class PhotoWindowViewModel : INotifyPropertyChanged
    {
        private string _filePath = string.Empty; // Initialize to avoid null
        private BitmapImage? _imageSource; // Nullable for initial state

        public string FilePath
        {
            get => _filePath;
            set
            {
                if (_filePath != value)
                {
                    _filePath = value;
                    OnPropertyChanged();
                    LoadImage(); // Reload image when file path changes
                }
            }
        }

        public BitmapImage? ImageSource
        {
            get => _imageSource;
            private set
            {
                _imageSource = value;
                OnPropertyChanged();
            }
        }

        public void LoadImage()
        {
            if (File.Exists(FilePath))
            {
                ImageSource = new BitmapImage(new Uri(FilePath));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}