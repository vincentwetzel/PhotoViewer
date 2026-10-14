using PhotoViewer.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PhotoViewer.ViewModels;

public class SourceItemViewModel : INotifyPropertyChanged
{
    public IPhotoProvider Provider { get; }
    public IProviderCapabilities Capabilities { get; }

    public SourceItemViewModel(IPhotoProvider provider)
    {
        Provider = provider;
        Capabilities = ProviderCapabilities.For(provider);
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

    public string StatusText => ProviderStatusFormatter.GetStatus(Provider, LoadState, LastLoadedAt);

    private string _loadState = "Ready";
    public string LoadState
    {
        get => _loadState;
        private set
        {
            if (_loadState == value) return;
            _loadState = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(IsUnavailable));
        }
    }

    private string _lastError = string.Empty;
    public string LastError
    {
        get => _lastError;
        private set { if (_lastError == value) return; _lastError = value; OnPropertyChanged(); }
    }

    private DateTime? _lastLoadedAt;
    public DateTime? LastLoadedAt
    {
        get => _lastLoadedAt;
        private set { _lastLoadedAt = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); }
    }

    public bool IsUnavailable => LoadState is "Unavailable" or "Offline";

    public void MarkLoadSucceeded()
    {
        LastError = string.Empty;
        LastLoadedAt = DateTime.UtcNow;
        LoadState = "Connected";
    }

    public void MarkLoadFailed(Exception exception)
    {
        LastError = exception.Message;
        LoadState = "Unavailable";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
