using PhotoViewer.Models;
using System.ComponentModel;
using System;

namespace PhotoViewer.ViewModels
{
    public class PhotoItemViewModel : INotifyPropertyChanged
    {
        public PhotoItem Photo { get; }

        private bool _isDuplicate;
        public bool IsDuplicate
        {
            get => _isDuplicate;
            set
            {
                if (_isDuplicate == value) return;
                _isDuplicate = value;
                OnPropertyChanged(nameof(IsDuplicate));
            }
        }

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

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        private string _dateGroupLabel = string.Empty;
        public string DateGroupLabel
        {
            get => _dateGroupLabel;
            private set
            {
                if (_dateGroupLabel == value) return;
                _dateGroupLabel = value;
                OnPropertyChanged(nameof(DateGroupLabel));
                OnPropertyChanged(nameof(HasDateGroupHeader));
            }
        }

        public bool HasDateGroupHeader => !string.IsNullOrWhiteSpace(DateGroupLabel);

        public void SetDateGroupLabel(string? label) => DateGroupLabel = label ?? string.Empty;

        public PhotoItemViewModel(PhotoItem photo)
        {
            Photo = photo;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
