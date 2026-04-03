using PhotoViewer.Models;
using PhotoViewer.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Timers;

namespace PhotoViewer.ViewModels
{
    /// <summary>
    /// Represents a local folder source in the navigation tree.
    /// Exposes a tree of FolderNode items for expand/collapse browsing.
    /// Watches the file system for changes and refreshes the tree in real-time.
    /// </summary>
    public class FolderSourceViewModel : INotifyPropertyChanged, IDisposable
    {
        public LocalFolderProvider Provider { get; }
        public FolderNode Root { get; }

        public string DisplayName { get; }

        /// <summary>
        /// Total photo count across all folders and subfolders.
        /// </summary>
        public int PhotoCount => Root.TotalPhotoCount;

        /// <summary>
        /// Refreshes the folder tree recursively, picking up any folders added/deleted on disk.
        /// </summary>
        public void RefreshTree()
        {
            Root.RefreshSubFoldersRecursive();
        }

        private object? _selectedItem;
        /// <summary>
        /// The currently selected item — either a FolderNode or this root itself.
        /// When null or the root, the entire folder tree (all subfolders) is used.
        /// When a FolderNode, that folder and its subfolders are used.
        /// </summary>
        public object? SelectedItem
        {
            get => _selectedItem;
            set { _selectedItem = value; OnPropertyChanged(); }
        }

        public FolderSourceViewModel(LocalFolderProvider provider)
        {
            Provider = provider;
            // Create root with RootSource set before children are loaded
            Root = new FolderNode(provider.SourceName);
            Root.RootSource = this;
            // Re-load subfolders now that RootSource is set, so children inherit it
            Root.LoadSubFolders();
            DisplayName = System.IO.Path.GetFileName(provider.SourceName);

            // Start watching for file system changes
            StartFileSystemWatcher();
        }

        private FileSystemWatcher? _watcher;
        private readonly System.Timers.Timer _refreshTimer = new(500) { AutoReset = false };
        private volatile bool _pendingRefresh;

        private void StartFileSystemWatcher()
        {
            if (!Directory.Exists(Provider.SourceName)) return;

            _watcher = new FileSystemWatcher(Provider.SourceName)
            {
                NotifyFilter = NotifyFilters.DirectoryName,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            _watcher.Created += OnFolderChanged;
            _watcher.Deleted += OnFolderChanged;
            _watcher.Renamed += OnFolderChanged;

            _refreshTimer.Elapsed += OnRefreshTimer;
        }

        private void OnFolderChanged(object sender, FileSystemEventArgs e)
        {
            _pendingRefresh = true;
            // Debounce: restart timer on every event so we batch rapid-fire changes
            _refreshTimer.Stop();
            _refreshTimer.Start();
        }

        private void OnRefreshTimer(object? sender, ElapsedEventArgs e)
        {
            if (!_pendingRefresh) return;
            _pendingRefresh = false;

            // Refresh on the UI thread
            System.Windows.Application.Current.Dispatcher.Invoke(() => RefreshTree());
        }

        public void Dispose()
        {
            if (_watcher != null)
            {
                _watcher.Created -= OnFolderChanged;
                _watcher.Deleted -= OnFolderChanged;
                _watcher.Renamed -= OnFolderChanged;
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }
            _refreshTimer.Stop();
            _refreshTimer.Dispose();
        }

        /// <summary>
        /// Gets all photo file paths for the currently selected item.
        /// If nothing specific is selected (root level), returns all photos in the tree.
        /// If a FolderNode is selected, returns photos from that folder and all its subfolders.
        /// </summary>
        public IEnumerable<string> GetSelectedPhotoPaths()
        {
            if (_selectedItem is FolderNode node)
            {
                return node.GetAllPhotoPaths();
            }

            // Default: all photos in the entire folder tree
            return Root.GetAllPhotoPaths();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
