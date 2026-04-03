using PhotoViewer.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PhotoViewer.ViewModels
{
    public class SourceItemViewModel : INotifyPropertyChanged
    {
        public IPhotoProvider Provider { get; }

        public SourceItemViewModel(IPhotoProvider provider)
        {
            Provider = provider;
        }

        private string _displayName = string.Empty;

        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(); }
        }

        private int _photoCount;
        public int PhotoCount
        {
            get => _photoCount;
            set { _photoCount = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}