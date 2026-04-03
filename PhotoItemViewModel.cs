using PhotoViewer.Models;
using System.ComponentModel;

namespace PhotoViewer.ViewModels
{
    public class PhotoItemViewModel : INotifyPropertyChanged
    {
        public PhotoItem Photo { get; }

        private bool _isFavorite;
        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                if (_isFavorite == value) return;
                _isFavorite = value;
                OnPropertyChanged(nameof(IsFavorite));
            }
        }

        public PhotoItemViewModel(PhotoItem photo)
        {
            Photo = photo;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}