using PhotoViewer.Commands;
using PhotoViewer.Models;
using PhotoViewer.Services;
using PhotoViewer.ViewModels;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System;
using System.Linq;
using System.Windows.Data;
using System.Windows;
using System.Diagnostics;
using Microsoft.Win32;
using Microsoft.VisualBasic.FileIO;
using System.Collections.Specialized;

namespace PhotoViewer.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public ICommand OpenImageCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand SaveLayoutCommand { get; }
        public ICommand LoadLayoutCommand { get; }
        public ICommand AddLocalFolderCommand { get; }
        public ICommand ToggleFavoriteCommand { get; }
        public ICommand RemoveSourceCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand EnterSelectionModeCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand SelectNoneCommand { get; }
        public ICommand CancelSelectionCommand { get; }
        public ICommand ToggleSelectedFavoritesCommand { get; }
        public ICommand RetryLoadCommand { get; }
        public ICommand DeleteSelectedCommand { get; }
        public ICommand CopySelectedCommand { get; }
        public ICommand OpenWithSelectedCommand { get; }
        public ICommand MoveSelectedCommand { get; }
        public ICommand ToggleNavigationCommand { get; }

        private string _selectedTheme = "System";
        public string SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (_selectedTheme == value) return;
                _selectedTheme = value;
                OnPropertyChanged(nameof(SelectedTheme));
                ApplyTheme();
            }
        }

        private readonly ObservableCollection<PhotoWindowViewModel> _openPhotoWindows = new();
        private readonly List<PhotoWindow> _openPhotoWindowsList = new();
        private readonly LayoutService _layoutService;
        private readonly PhotoWindowSizeService _photoWindowSizeService;
        private readonly MainWindowSizeService _mainWindowSizeService;
        private CancellationTokenSource? _photoLoadingCts;
        private readonly FavoritesService _favoritesService;
        private readonly HistoryService _historyService;
        private readonly SourcePersistenceService _sourcePersistenceService;
        private readonly SettingsService _settingsService;

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading == value) return;
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
                OnPropertyChanged(nameof(HasNoResults));
            }
        }

        /// <summary>Provides access to the main window size service for the MainWindow.</summary>
        public MainWindowSizeService MainWindowSizeService => _mainWindowSizeService;

        /// <summary>Caches loaded photo items per source to avoid re-scanning on every switch.</summary>
        private readonly Dictionary<object, PhotoCacheEntry> _photoCache = new();
        /// <summary>How long before a cached source is considered stale and re-scanned (5 minutes).</summary>
        private static readonly TimeSpan CacheStalenessThreshold = TimeSpan.FromMinutes(5);

        private class PhotoCacheEntry
        {
            public List<PhotoItem> Items { get; set; } = new();
            /// <summary>Fully-built view models, ready for instant display.</summary>
            public List<PhotoItemViewModel>? ViewModels { get; set; }
            public HashSet<string> FilePaths { get; set; } = new();
            public DateTime CachedAt { get; set; }

            /// <summary>
            /// Checks if any cached files have been moved or deleted.
            /// Returns true if the cache is stale.
            /// </summary>
            public bool IsStale()
            {
                if ((DateTime.UtcNow - CachedAt) > CacheStalenessThreshold)
                    return true;

                // Quick check: if more than 10% of files are missing, consider stale
                if (FilePaths.Count == 0) return true;

                int missing = 0;
                int checkLimit = Math.Min(FilePaths.Count, 50); // Check up to 50 files
                foreach (var path in FilePaths.Take(checkLimit))
                {
                    if (!File.Exists(path))
                        missing++;
                }

                // If more than 10% of sampled files are missing, consider stale
                return missing > checkLimit * 0.1;
            }
        }

        /// <summary>All sources (used for aggregation in Gallery, persistence).</summary>
        public ObservableCollection<object> Sources { get; } = new();
        /// <summary>Collection sources: Gallery, Favorites, Recently Viewed.</summary>
        public ObservableCollection<SourceItemViewModel> CollectionSources { get; } = new();
        /// <summary>User sources: local folder trees.</summary>
        public ObservableCollection<object> UserSources { get; } = new();

        /// <summary>The photo collection displayed in the gallery view. Uses RangeObservableCollection for instant batch updates.</summary>
        private readonly RangeObservableCollection<PhotoItemViewModel> _photos;
        public ICollectionView PhotosView { get; }

        /// <summary>Batch size for progressive UI updates when loading large sources.</summary>
        private const int UiUpdateBatchSize = 200;

        /// <summary>Total count of ALL photos from the current source (including those not displayed).</summary>
        private int _totalPhotoCount;
        public int TotalPhotoCount
        {
            get => _totalPhotoCount;
            set
            {
                _totalPhotoCount = value;
                OnPropertyChanged(nameof(TotalPhotoCount));
                OnPropertyChanged(nameof(PhotoSummary));
            }
        }

        private object? _selectedSource;
        public object? SelectedSource
        {
            get => _selectedSource;
            set
            {
                if (_selectedSource == value) return;
                _selectedSource = value;
                OnPropertyChanged(nameof(SelectedSource));
                OnPropertyChanged(nameof(SearchScope));
                OnPropertyChanged(nameof(GalleryTitle));
                OnPropertyChanged(nameof(PhotoSummary));
                OnPropertyChanged(nameof(DateRangeSummary));
                OnPropertyChanged(nameof(LoadErrorTitle));
                LoadPhotosForSelectedSourceAsync();
            }
        }

        /// <summary>
        /// Forces a reload of photos for the currently selected source, even if it's the same instance.
        /// Used when a subfolder within the same folder tree changes selection.
        /// </summary>
        public void ReloadCurrentSource()
        {
            LoadPhotosForSelectedSourceAsync();
        }

        public List<string> SortOptions { get; } = new List<string> { "Date taken", "Date created", "Date modified", "Name", "File size" };

        private string _selectedSortOption = "Name";
        public string SelectedSortOption
        {
            get => _selectedSortOption;
            set
            {
                if (_selectedSortOption == value) return;
                _selectedSortOption = value;
                OnPropertyChanged(nameof(SelectedSortOption));
                ApplySort();
                SaveGalleryPreferences();
            }
        }

        public List<string> SortDirections { get; } = new List<string> { "Ascending", "Descending" };

        private string _selectedSortDirection = "Ascending";
        public string SelectedSortDirection
        {
            get => _selectedSortDirection;
            set
            {
                if (_selectedSortDirection == value) return;
                _selectedSortDirection = value;
                OnPropertyChanged(nameof(SelectedSortDirection));
                ApplySort();
                SaveGalleryPreferences();
            }
        }

        private string _searchText = string.Empty;
        private readonly System.Windows.Threading.DispatcherTimer _searchDebounceTimer =
            new(System.Windows.Threading.DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(250) };
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                OnPropertyChanged(nameof(HasSearchText));
                OnPropertyChanged(nameof(EmptyStateTitle));
                OnPropertyChanged(nameof(EmptyStateMessage));
                _searchDebounceTimer.Stop();
                _searchDebounceTimer.Start();
            }
        }

        public bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);

        public bool HasNoSearchResults => HasSearchText && PhotosView.Cast<object>().Count() == 0;

        public bool HasNoResults => !IsLoading && PhotosView.Cast<object>().Count() == 0;

        public string EmptyStateTitle => HasSearchText ? "No matching photos" : "No photos in this collection";

        public string EmptyStateMessage => HasSearchText
            ? "Try a different name, date, or media type."
            : "Add a folder or import photos to start building your gallery.";

        private string _loadErrorMessage = string.Empty;
        public string LoadErrorMessage
        {
            get => _loadErrorMessage;
            private set { _loadErrorMessage = value; OnPropertyChanged(nameof(LoadErrorMessage)); OnPropertyChanged(nameof(HasLoadError)); }
        }

        public bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);

        public string LoadErrorTitle => "This local source is unavailable";

        private string _loadWarningMessage = string.Empty;
        public string LoadWarningMessage
        {
            get => _loadWarningMessage;
            private set { _loadWarningMessage = value; OnPropertyChanged(nameof(LoadWarningMessage)); OnPropertyChanged(nameof(HasLoadWarning)); }
        }

        public bool HasLoadWarning => !string.IsNullOrWhiteSpace(LoadWarningMessage);

        public List<string> MediaTypeOptions { get; } = new() { "All media", "Photos", "Videos" };

        private string _selectedMediaType = "All media";
        public string SelectedMediaType
        {
            get => _selectedMediaType;
            set
            {
                if (_selectedMediaType == value) return;
                _selectedMediaType = value;
                OnPropertyChanged(nameof(SelectedMediaType));
                PhotosView.Refresh();
                UpdateDateGrouping();
                OnPropertyChanged(nameof(HasNoSearchResults));
                OnPropertyChanged(nameof(HasNoResults));
                SaveGalleryPreferences();
            }
        }

        public List<double> ThumbnailSizeOptions { get; } = new() { 140, 180, 230 };

        private double _thumbnailRowHeight = 180;
        public double ThumbnailRowHeight
        {
            get => _thumbnailRowHeight;
            set
            {
                if (Math.Abs(_thumbnailRowHeight - value) < 0.1) return;
                _thumbnailRowHeight = value;
                OnPropertyChanged(nameof(ThumbnailRowHeight));
                SaveGalleryPreferences();
            }
        }

        private bool _confirmDelete = true;
        public bool ConfirmDelete
        {
            get => _confirmDelete;
            set { if (_confirmDelete == value) return; _confirmDelete = value; OnPropertyChanged(nameof(ConfirmDelete)); SaveGalleryPreferences(); }
        }

        private bool _mouseWheelZoom = true;
        public bool MouseWheelZoom
        {
            get => _mouseWheelZoom;
            set { if (_mouseWheelZoom == value) return; _mouseWheelZoom = value; OnPropertyChanged(nameof(MouseWheelZoom)); SaveGalleryPreferences(); }
        }

        private bool _showFileLocation = true;
        public bool ShowFileLocation
        {
            get => _showFileLocation;
            set { if (_showFileLocation == value) return; _showFileLocation = value; OnPropertyChanged(nameof(ShowFileLocation)); SaveGalleryPreferences(); }
        }

        public List<double> ViewerZoomOptions { get; } = new() { 1.0, 1.5, 2.0 };

        private double _defaultViewerZoom = 1.0;
        public double DefaultViewerZoom
        {
            get => _defaultViewerZoom;
            set
            {
                if (Math.Abs(_defaultViewerZoom - value) < 0.01) return;
                _defaultViewerZoom = value;
                OnPropertyChanged(nameof(DefaultViewerZoom));
                SaveGalleryPreferences();
            }
        }

        private bool _allowAnonymousDiagnostics;
        public bool AllowAnonymousDiagnostics
        {
            get => _allowAnonymousDiagnostics;
            set { if (_allowAnonymousDiagnostics == value) return; _allowAnonymousDiagnostics = value; OnPropertyChanged(nameof(AllowAnonymousDiagnostics)); SaveGalleryPreferences(); }
        }

        private bool _personalizedExperiences;
        public bool PersonalizedExperiences
        {
            get => _personalizedExperiences;
            set { if (_personalizedExperiences == value) return; _personalizedExperiences = value; OnPropertyChanged(nameof(PersonalizedExperiences)); SaveGalleryPreferences(); }
        }

        private bool _reduceMotion;
        public bool ReduceMotion
        {
            get => _reduceMotion;
            set { if (_reduceMotion == value) return; _reduceMotion = value; OnPropertyChanged(nameof(ReduceMotion)); SaveGalleryPreferences(); }
        }

        private bool _isSelectionMode;
        public bool IsSelectionMode
        {
            get => _isSelectionMode;
            private set
            {
                if (_isSelectionMode == value) return;
                _isSelectionMode = value;
                OnPropertyChanged(nameof(IsSelectionMode));
            }
        }

        public int SelectedPhotoCount => PhotoSelectionService.Count(_photos);

        public string GalleryTitle => SelectedSource is SourceItemViewModel source
            ? source.DisplayName
            : "Gallery";

        public string SearchScope => SelectedSource is SourceItemViewModel source
            ? $"Search {source.DisplayName}"
            : "Search photos";

        public string PhotoSummary
        {
            get
            {
                var photos = _photos.Count(item => !item.Photo.IsVideo);
                var videos = _photos.Count(item => item.Photo.IsVideo);
                if (_totalPhotoCount > 0 && _photos.Count == 0)
                    return $"{_totalPhotoCount} items";
                if (videos == 0) return $"{photos} photos";
                if (photos == 0) return $"{videos} videos";
                return $"{photos} photos · {videos} videos";
            }
        }

        public string DateRangeSummary
        {
            get
            {
                var dates = _photos.Select(item => item.Photo.DateTaken)
                    .Where(date => date != default)
                    .Select(date => date.ToLocalTime().Date)
                    .Distinct()
                    .OrderBy(date => date)
                    .ToList();
                if (dates.Count == 0) return "Date unavailable";
                if (dates[0] == dates[^1]) return dates[0].ToString("MMMM d, yyyy");
                return $"{dates[0]:MMM d, yyyy} – {dates[^1]:MMM d, yyyy}";
            }
        }

        private bool _isNavigationPaneCollapsed;
        public bool IsNavigationPaneCollapsed
        {
            get => _isNavigationPaneCollapsed;
            private set
            {
                if (_isNavigationPaneCollapsed == value) return;
                _isNavigationPaneCollapsed = value;
                OnPropertyChanged(nameof(IsNavigationPaneCollapsed));
                OnPropertyChanged(nameof(NavigationPaneWidth));
            }
        }

        public System.Windows.GridLength NavigationPaneWidth =>
            new(IsNavigationPaneCollapsed ? 72 : 280, System.Windows.GridUnitType.Pixel);

        public MainWindowViewModel()
        {
            OpenImageCommand = new RelayCommand(ExecuteOpenImageCommand);
            ImportCommand = new RelayCommand(_ => ExecuteImportCommandAsync());
            SaveLayoutCommand = new RelayCommand(ExecuteSaveLayoutCommand);
            LoadLayoutCommand = new RelayCommand(ExecuteLoadLayoutCommand);
            AddLocalFolderCommand = new RelayCommand(ExecuteAddLocalFolderCommand);
            ToggleFavoriteCommand = new RelayCommand(ExecuteToggleFavoriteCommand);
            RemoveSourceCommand = new RelayCommand(ExecuteRemoveSourceCommand, CanExecuteRemoveSourceCommand);
            OpenSettingsCommand = new RelayCommand(ExecuteOpenSettingsCommand);
            ClearSearchCommand = new RelayCommand(_ => SearchText = string.Empty);
            EnterSelectionModeCommand = new RelayCommand(_ => IsSelectionMode = true);
            SelectAllCommand = new RelayCommand(_ => SelectAllPhotos());
            SelectNoneCommand = new RelayCommand(_ => SelectNonePhotos());
            CancelSelectionCommand = new RelayCommand(_ => CancelSelection());
            ToggleSelectedFavoritesCommand = new RelayCommand(_ => ToggleSelectedFavorites());
            RetryLoadCommand = new RelayCommand(_ => ReloadCurrentSource());
            DeleteSelectedCommand = new RelayCommand(_ => DeleteSelectedPhotos());
            CopySelectedCommand = new RelayCommand(_ => CopySelectedPhotos());
            OpenWithSelectedCommand = new RelayCommand(_ => OpenWithSelectedPhoto());
            MoveSelectedCommand = new RelayCommand(_ => MoveSelectedPhotos());
            ToggleNavigationCommand = new RelayCommand(_ => IsNavigationPaneCollapsed = !IsNavigationPaneCollapsed);

            _layoutService = new LayoutService();
            _photoWindowSizeService = new PhotoWindowSizeService();
            _mainWindowSizeService = new MainWindowSizeService();
            _favoritesService = new FavoritesService();
            _historyService = new HistoryService();
            _sourcePersistenceService = new SourcePersistenceService();
            _settingsService = new SettingsService();

            // Load saved settings
            var settings = _settingsService.LoadSettings();
            _selectedTheme = settings.Theme;
            _selectedSortOption = settings.SortOption switch
            {
                "Date taken" or "Date created" or "Date modified" or "Name" or "File size" => settings.SortOption,
                "File Name" => "Name",
                "Date Created" => "Date created",
                "File Size" => "File size",
                _ => "Name"
            };
            _selectedSortDirection = SortDirections.Contains(settings.SortDirection) ? settings.SortDirection : "Ascending";
            _selectedMediaType = MediaTypeOptions.Contains(settings.MediaType) ? settings.MediaType : "All media";
            _thumbnailRowHeight = ThumbnailSizeOptions.Contains(settings.ThumbnailRowHeight) ? settings.ThumbnailRowHeight : 180;
            _confirmDelete = settings.ConfirmDelete;
            _mouseWheelZoom = settings.MouseWheelZoom;
            _showFileLocation = settings.ShowFileLocation;
            _allowAnonymousDiagnostics = settings.AllowAnonymousDiagnostics;
            _personalizedExperiences = settings.PersonalizedExperiences;
            _reduceMotion = settings.ReduceMotion;
            _defaultViewerZoom = ViewerZoomOptions.Contains(settings.DefaultViewerZoom) ? settings.DefaultViewerZoom : 1.0;

            _photos = new RangeObservableCollection<PhotoItemViewModel>();
            PhotosView = CollectionViewSource.GetDefaultView(_photos);
            PhotosView.Filter = FilterPhotos;
            _photos.CollectionChanged += (_, _) =>
            {
                UpdateDuplicateIndicators();
                UpdateDateGrouping();
                OnPropertyChanged(nameof(HasNoSearchResults));
                OnPropertyChanged(nameof(HasNoResults));
                OnPropertyChanged(nameof(PhotoSummary));
                OnPropertyChanged(nameof(DateRangeSummary));
            };
            _searchDebounceTimer.Tick += (_, _) =>
            {
                _searchDebounceTimer.Stop();
                PhotosView.Refresh();
                UpdateDateGrouping();
                OnPropertyChanged(nameof(HasNoSearchResults));
                OnPropertyChanged(nameof(HasNoResults));
            };
            ApplySort();
        }

        public void NotifySelectionChanged()
        {
            OnPropertyChanged(nameof(SelectedPhotoCount));
        }

        private void SelectAllPhotos()
        {
            PhotoSelectionService.SelectAll(_photos);
            NotifySelectionChanged();
        }

        private void UpdateDuplicateIndicators()
        {
            var duplicateNames = _photos
                .GroupBy(photo => photo.Photo.FileName, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var photo in _photos)
                photo.IsDuplicate = duplicateNames.Contains(photo.Photo.FileName);
        }

        private void CancelSelection()
        {
            SelectNonePhotos();
            IsSelectionMode = false;
        }

        private void SelectNonePhotos()
        {
            PhotoSelectionService.Clear(_photos);
            NotifySelectionChanged();
        }

        private void ToggleSelectedFavorites()
        {
            var selected = _photos.Where(photo => photo.IsSelected).ToList();
            if (selected.Count == 0) return;

            var shouldFavorite = selected.Any(photo => !photo.IsFavorite);
            foreach (var photo in selected)
            {
                photo.IsFavorite = shouldFavorite;
                if (shouldFavorite)
                    _favoritesService.AddFavorite(photo.Photo.FilePath);
                else
                    _favoritesService.RemoveFavorite(photo.Photo.FilePath);
            }

            _ = UpdateFavoritesCountAsync();
        }

        private List<PhotoItemViewModel> GetSelectedPhotos() => _photos.Where(photo => photo.IsSelected).ToList();

        private List<PhotoItemViewModel> GetSelectedLocalPhotos() => GetSelectedPhotos()
            .Where(photo => File.Exists(photo.Photo.FilePath)).ToList();

        private IProviderCapabilities SelectedProviderCapabilities => SelectedSource switch
        {
            FolderSourceViewModel folder => folder.Capabilities,
            SourceItemViewModel source => source.Capabilities,
            _ => new ProviderCapabilitySet(false, false, false, false, false, true)
        };

        private void DeleteSelectedPhotos()
        {
            var selected = GetSelectedPhotos();
            var localPhotos = selected.Where(photo => File.Exists(photo.Photo.FilePath)).ToList();
            if (!SelectedProviderCapabilities.CanDelete || localPhotos.Count == 0)
            {
                System.Windows.MessageBox.Show("Delete is unavailable for the selected provider items.", "Delete", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var settings = _settingsService.LoadSettings();
            if (settings.ConfirmDelete)
            {
                var result = MessageBox.Show($"Move {localPhotos.Count} item(s) to the Recycle Bin?", "Delete photos",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) return;
            }

            foreach (var photo in localPhotos)
            {
                try
                {
                    FileSystem.DeleteFile(photo.Photo.FilePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    _photos.Remove(photo);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Bulk delete failed for {photo.Photo.FilePath}: {ex.Message}");
                }
            }

            if (selected.Count > localPhotos.Count)
                LoadWarningMessage = "Some selected items are unavailable for local deletion.";
            CancelSelection();
        }

        private void CopySelectedPhotos()
        {
            var localPhotos = GetSelectedLocalPhotos();
            if (localPhotos.Count == 0)
            {
                MessageBox.Show("Copy is unavailable for the selected provider items.", "Copy", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var paths = new StringCollection();
            foreach (var photo in localPhotos) paths.Add(photo.Photo.FilePath);
            Clipboard.SetFileDropList(paths);
            if (localPhotos.Count < GetSelectedPhotos().Count)
                LoadWarningMessage = "Only local files were copied to the clipboard.";
        }

        private void OpenWithSelectedPhoto()
        {
            var photo = GetSelectedLocalPhotos().FirstOrDefault();
            if (photo == null)
            {
                MessageBox.Show("Open with is unavailable for the selected provider items.", "Open with", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "rundll32.exe",
                Arguments = $"shell32.dll,OpenAs_RunDLL \"{photo.Photo.FilePath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }

        private void MoveSelectedPhotos()
        {
            var localPhotos = GetSelectedLocalPhotos();
            if (!SelectedProviderCapabilities.CanMove || localPhotos.Count == 0)
            {
                MessageBox.Show("Move is unavailable for the selected provider items.", "Move", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Choose a destination folder",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            foreach (var photo in localPhotos)
            {
                try
                {
                    var destination = GetUniqueDestinationPath(dialog.SelectedPath, photo.Photo.FileName);
                    File.Move(photo.Photo.FilePath, destination);
                    _photos.Remove(photo);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Bulk move failed for {photo.Photo.FilePath}: {ex.Message}");
                }
            }
            CancelSelection();
        }

        private static string GetUniqueDestinationPath(string folder, string fileName)
        {
            var path = Path.Combine(folder, fileName);
            if (!File.Exists(path)) return path;

            var name = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var index = 2;
            do
            {
                path = Path.Combine(folder, $"{name} ({index++}){extension}");
            } while (File.Exists(path));
            return path;
        }

        public async Task InitializeAsync()
        {
            try
            {
                AddDefaultSources();
                LoadPersistedSources();

                if (Sources.OfType<FolderSourceViewModel>().Count() == 0)
                {
                    await AddDefaultPicturesFolderAsync();
                }

                // Photo counts are calculated on-demand when user expands/clicks folders.
                // NOT on startup — recursive file enumeration for large sources causes disk I/O saturation.
                // _ = Task.Run(async () => await LoadCollectionSourceCountsAsync());

                // Restore previously saved layout on startup (if it exists)
                RestoreLayoutOnStartup();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Startup error: {ex.Message}\n\n{ex.StackTrace}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Restores the last saved layout on application startup.
        /// </summary>
        private void RestoreLayoutOnStartup()
        {
            if (!_layoutService.HasDefaultLayout())
                return;

            var layout = _layoutService.LoadDefaultLayout();
            if (layout == null || layout.PhotoWindows.Count == 0)
                return;

            // Restore each window with a small delay to avoid overwhelming the system
            foreach (var windowState in layout.PhotoWindows)
            {
                if (windowState == null || string.IsNullOrEmpty(windowState.FilePath))
                    continue;

                if (!File.Exists(windowState.FilePath))
                    continue;

                OpenPhotoWindowFromState(windowState);
            }
        }

        /// <summary>
        /// Computes and sets photo counts for all collection sources (Gallery, Favorites, Recently Viewed).
        /// For Gallery, uses a fast estimate instead of full enumeration to avoid freezing on large sources.
        /// </summary>
        private async Task LoadCollectionSourceCountsAsync()
        {
            foreach (var collection in CollectionSources)
            {
                try
                {
                    if (collection.DisplayName == "Gallery")
                    {
                        // Don't enumerate all 25,000 files on startup - show "..." instead
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => collection.PhotoCount = -1); // -1 means "loading/unknown"
                    }
                    else
                    {
                        var photos = await collection.Provider.GetPhotoPathsAsync();
                        var count = photos.Count();
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => collection.PhotoCount = count);
                    }
                }
                catch { }
            }
        }

        private void AddDefaultSources()
        {
            // Add Gallery as the first collection source
            var galleryProvider = new GalleryProvider(Enumerable.Empty<IPhotoProvider>());
            var gallerySource = new SourceItemViewModel(galleryProvider) { DisplayName = "Gallery" };
            CollectionSources.Add(gallerySource);

            var favoritesProvider = new FavoritesProvider(_favoritesService);
            var favoritesSource = new SourceItemViewModel(favoritesProvider) { DisplayName = "Favorites" };
            CollectionSources.Add(favoritesSource);

            var recentlyViewedProvider = new RecentlyViewedProvider(_historyService);
            var recentlyViewedSource = new SourceItemViewModel(recentlyViewedProvider) { DisplayName = "Recently Viewed" };
            CollectionSources.Add(recentlyViewedSource);

        }

        private void ExecuteSaveLayoutCommand(object? parameter)
        {
            if (_openPhotoWindowsList.Count == 0)
            {
                System.Windows.MessageBox.Show("No photo windows are currently open.", "Save Layout", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save Workspace Layout",
                Filter = "JSON Files|*.json|All Files|*.*",
                DefaultExt = "json",
                FileName = "PhotoViewerLayout.json"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var layout = _layoutService.CollectLayout(_openPhotoWindowsList);
                    _layoutService.SaveLayout(layout, saveDialog.FileName);
                    // Also save as the default layout for auto-restore on next startup
                    _layoutService.SaveDefaultLayout(layout);
                    System.Windows.MessageBox.Show($"Layout saved successfully.\n\n{layout.PhotoWindows.Count} window(s) saved.", "Save Layout", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to save layout: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteLoadLayoutCommand(object? parameter)
        {
            var openDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Load Workspace Layout",
                Filter = "JSON Files|*.json|All Files|*.*",
                DefaultExt = "json",
                Multiselect = false
            };

            if (openDialog.ShowDialog() == true)
            {
                LoadLayoutFromFile(openDialog.FileName);
            }
        }

        /// <summary>
        /// Loads a layout from a JSON file and restores all PhotoWindows.
        /// </summary>
        private void LoadLayoutFromFile(string filePath)
        {
            var layout = _layoutService.LoadLayout(filePath);
            if (layout == null || layout.PhotoWindows.Count == 0)
            {
                System.Windows.MessageBox.Show("No valid layout data found in the file.", "Load Layout", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Close all currently open photo windows
            CloseAllPhotoWindows();

            int restoredCount = 0;
            int skippedCount = 0;

            foreach (var windowState in layout.PhotoWindows)
            {
                if (windowState == null || string.IsNullOrEmpty(windowState.FilePath))
                    continue;

                if (!File.Exists(windowState.FilePath))
                {
                    skippedCount++;
                    continue;
                }

                OpenPhotoWindowFromState(windowState);
                restoredCount++;
            }

            var message = $"Layout restored successfully.\n\n{restoredCount} window(s) opened.";
            if (skippedCount > 0)
            {
                message += $"\n{skippedCount} file(s) not found and were skipped.";
            }
            System.Windows.MessageBox.Show(message, "Load Layout", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Opens a single PhotoWindow from saved state.
        /// </summary>
        private void OpenPhotoWindowFromState(PhotoWindowState state)
        {
            var photoWindowViewModel = new PhotoWindowViewModel { FilePath = state.FilePath };
            var photoWindow = new PhotoWindow
            {
                DataContext = photoWindowViewModel
            };

            // Apply saved transform state
            photoWindow.LoadImage(state.FilePath, state.ZoomLevel, state.PanOffsetX, state.PanOffsetY);

            // Apply saved window position and size
            photoWindow.Left = state.Left;
            photoWindow.Top = state.Top;
            photoWindow.Width = state.Width;
            photoWindow.Height = state.Height;

            // Track this window
            _openPhotoWindows.Add(photoWindowViewModel);
            _openPhotoWindowsList.Add(photoWindow);

            // Handle window close
            photoWindow.Closed += (sender, e) =>
            {
                _openPhotoWindows.Remove(photoWindowViewModel);
                _openPhotoWindowsList.Remove(photoWindow);
            };

            photoWindow.Show();

            // Maximize if it was saved as maximized
            // Use Dispatcher to ensure window is fully shown first
            if (state.IsMaximized)
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    photoWindow.WindowState = WindowState.Maximized;
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        /// <summary>
        /// Closes all currently open photo windows.
        /// </summary>
        private void CloseAllPhotoWindows()
        {
            var windows = _openPhotoWindowsList.ToList(); // Copy to avoid modification during iteration
            foreach (var window in windows)
            {
                try
                {
                    window.Close();
                }
                catch { /* Ignore errors during close */ }
            }
            _openPhotoWindowsList.Clear();
            _openPhotoWindows.Clear();
        }

        /// <summary>
        /// Auto-saves the current layout when the main window is closing.
        /// </summary>
        public void SaveCurrentLayout()
        {
            if (_openPhotoWindowsList.Count == 0)
            {
                // If no windows are open, clear any saved layout
                if (_layoutService.HasDefaultLayout())
                {
                    try
                    {
                        System.IO.File.Delete(System.IO.Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "PhotoViewer", "workspaceLayout.json"));
                    }
                    catch { }
                }
                return;
            }

            try
            {
                var layout = _layoutService.CollectLayout(_openPhotoWindowsList);
                _layoutService.SaveDefaultLayout(layout);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to auto-save layout on exit: {ex.Message}");
            }
        }

        private async Task AddDefaultPicturesFolderAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    var picturesFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    if (!string.IsNullOrWhiteSpace(picturesFolder))
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            var provider = new LocalFolderProvider(picturesFolder);
                            if (!Sources.Any(s => s is FolderSourceViewModel fsv && fsv.Provider.SourceName == provider.SourceName))
                            {
                                var newSource = new FolderSourceViewModel(provider);
                                Sources.Add(newSource);
                                UserSources.Add(newSource);
                                PersistSources();
                                
                                // Kick off async count in background
                                _ = newSource.CalculatePhotoCountAsync();
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error adding default pictures folder: {ex}");
                }
            });
        }

        private void LoadPersistedSources()
        {
            var sourceConfigs = _sourcePersistenceService.LoadSources();
            var folderViewModelsToCount = new List<FolderSourceViewModel>();

            foreach (var config in sourceConfigs)
            {
                try
                {
                    if (config.Type == "LocalFolder")
                    {
                        if (string.IsNullOrWhiteSpace(config.Path)) continue;

                        var provider = new LocalFolderProvider(config.Path);
                        if (!Sources.Any(s => s is FolderSourceViewModel fsv && fsv.Provider.SourceName == provider.SourceName))
                        {
                            var newSource = new FolderSourceViewModel(provider);
                            Sources.Add(newSource);
                            UserSources.Add(newSource);
                            folderViewModelsToCount.Add(newSource);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to load source config '{config.DisplayName}': {ex.Message}", "Source Load Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            // Photo counts are NOT calculated on startup — recursive file enumeration for large sources
            // saturates disk I/O and makes startup unbearably slow. Counts will appear as 0 until
            // the user interacts with the folder tree (counts are calculated on-demand when needed).
        }

        private void PersistSources()
        {
            try
            {
                var configsToSave = new List<SourceConfig>();
                foreach (var source in Sources)
                {
                    if (source is FolderSourceViewModel fsv)
                    {
                        configsToSave.Add(new SourceConfig
                        {
                            Type = "LocalFolder",
                            Path = fsv.Provider.SourceName,
                            DisplayName = fsv.DisplayName
                        });
                    }
                }
                _sourcePersistenceService.SaveSources(configsToSave);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error persisting sources: {ex}");
            }
        }

        private bool FilterPhotos(object item)
        {
            if (item is not PhotoItemViewModel vm) return false;
            return PhotoQueryMatcher.Matches(vm, SelectedMediaType, SearchText);
        }

        private void ApplySort()
        {
            PhotosView.SortDescriptions.Clear();

            var propertyName = PhotoSortDescriptor.GetPropertyName(_selectedSortOption);
            var direction = PhotoSortDescriptor.GetDirection(_selectedSortDirection);
            PhotosView.SortDescriptions.Add(new SortDescription(propertyName, direction));
            UpdateDateGrouping();
        }

        private void UpdateDateGrouping()
        {
            var dateSorting = _selectedSortOption is "Date taken" or "Date created" or "Date modified";
            foreach (var photo in _photos)
                photo.SetDateGroupLabel(string.Empty);
            if (!dateSorting) return;

            string? previousKey = null;
            foreach (var photo in PhotosView.Cast<PhotoItemViewModel>())
            {
                var date = PhotoSortDescriptor.GetGroupingDate(photo.Photo, _selectedSortOption);
                var key = PhotoDateGrouping.GetKey(date);
                if (key == previousKey) continue;
                photo.SetDateGroupLabel(PhotoDateGrouping.GetLabel(date));
                previousKey = key;
            }
        }

        private void ExecuteToggleFavoriteCommand(object? parameter)
        {
            if (parameter is not PhotoItemViewModel vm) return;

            vm.IsFavorite = !vm.IsFavorite;

            if (vm.IsFavorite)
                _favoritesService.AddFavorite(vm.Photo.FilePath);
            else
                _favoritesService.RemoveFavorite(vm.Photo.FilePath);

            // Update the favorites source count
            _ = UpdateFavoritesCountAsync();
        }

        private async Task UpdateFavoritesCountAsync()
        {
            var favoritesSource = CollectionSources.FirstOrDefault(s => s.DisplayName == "Favorites");
            if (favoritesSource == null) return;

            try
            {
                var photos = await favoritesSource.Provider.GetPhotoPathsAsync();
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    favoritesSource.PhotoCount = photos.Count());
            }
            catch { }
        }

        private bool CanExecuteRemoveSourceCommand(object? parameter)
        {
            return parameter is SourceItemViewModel { Provider: LocalFolderProvider }
                or FolderSourceViewModel;
        }

        private void ExecuteRemoveSourceCommand(object? parameter)
        {
            if (parameter is not SourceItemViewModel and not FolderSourceViewModel) return;

            var sourceName = parameter switch
            {
                SourceItemViewModel source => source.DisplayName,
                FolderSourceViewModel folder => folder.DisplayName,
                _ => "this source"
            };
            var confirmation = System.Windows.MessageBox.Show(
                $"Remove {sourceName} from PhotoViewer? Files will not be deleted.",
                "Remove source", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (confirmation != System.Windows.MessageBoxResult.Yes) return;

            if (parameter is FolderSourceViewModel fsv)
            {
                fsv.Dispose();
            }

            if (SelectedSource == parameter)
            {
                SelectedSource = Sources.FirstOrDefault(s => s != parameter);
            }

            Sources.Remove(parameter);
            UserSources.Remove(parameter);

            PersistSources();

            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }

        public void OpenImage(string filePath)
        {
            if (File.Exists(filePath))
            {
                _historyService.AddToHistory(filePath);

                var photoWindowViewModel = new PhotoWindowViewModel { FilePath = filePath };
                var photoWindow = new PhotoWindow
                {
                    DataContext = photoWindowViewModel
                };
                photoWindow.SetNavigationItems(PhotosView.Cast<PhotoItemViewModel>().Select(photo => photo.Photo.FilePath));

                // Apply saved window size
                var savedSize = _photoWindowSizeService.LoadSize();
                photoWindow.Width = savedSize.Width;
                photoWindow.Height = savedSize.Height;

                _openPhotoWindows.Add(photoWindowViewModel);
                _openPhotoWindowsList.Add(photoWindow);

                // Save window size when closed
                photoWindow.Closed += (sender, e) =>
                {
                    _openPhotoWindows.Remove(photoWindowViewModel);
                    _openPhotoWindowsList.Remove(photoWindow);
                    _photoWindowSizeService.SaveSize(photoWindow.Width, photoWindow.Height);
                };

                photoWindow.Show();
            }
            else
            {
                System.Windows.MessageBox.Show($"File not found: {filePath}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }


        private void ExecuteOpenImageCommand(object? parameter)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Image(s)",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filePath in openFileDialog.FileNames)
                {
                    OpenImage(filePath);
                }
            }
        }

        private void ExecuteAddLocalFolderCommand(object? parameter)
        {
            if (parameter is string droppedFolder && Directory.Exists(droppedFolder))
            {
                AddLocalFolderSource(droppedFolder);
                return;
            }

            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select a folder to add to the gallery",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                AddLocalFolderSource(dialog.SelectedPath);
            }
        }

        private void AddLocalFolderSource(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath)) return;
            if (Sources.Any(s => s is FolderSourceViewModel fsv &&
                                 fsv.Provider.SourceName.Equals(folderPath, StringComparison.OrdinalIgnoreCase))) return;

            var provider = new LocalFolderProvider(folderPath);
            var newSource = new FolderSourceViewModel(provider);
            Sources.Add(newSource);
            UserSources.Add(newSource);
            SelectedSource = newSource;
            PersistSources();
        }

        private async void LoadPhotosForSelectedSourceAsync()
        {
            _photoLoadingCts?.Cancel();
            _photoLoadingCts = new CancellationTokenSource();
            var cancellationToken = _photoLoadingCts.Token;

            IsLoading = true;
            LoadErrorMessage = string.Empty;
            LoadWarningMessage = string.Empty;

            try
            {
                // If Gallery is selected
                if (_selectedSource is SourceItemViewModel svm && svm.DisplayName == "Gallery")
                {
                    if (TryLoadFromCache("gallery", out var galleryItems) && !galleryItems.IsStale())
                    {
                        DisplayCachedItemsInstantly(galleryItems);
                        // Refresh in background
                        _ = RefreshSourceCacheAsync("gallery", () => LoadGalleryPhotosInternalAsync(cancellationToken));
                        return;
                    }
                    await LoadGalleryPhotosAsync(cancellationToken);
                    return;
                }

                // If FolderSourceViewModel is selected, get photos from selected folder node or entire tree
                if (_selectedSource is FolderSourceViewModel folderSource)
                {
                    var selectedNode = folderSource.SelectedItem as PhotoViewer.Models.FolderNode;
                    string cacheKey = selectedNode != null
                        ? $"folder:{folderSource.GetHashCode()}:{selectedNode.FullName}"
                        : $"folder:{folderSource.GetHashCode()}:root";

                    if (TryLoadFromCache(cacheKey, out var folderItems) && !folderItems.IsStale())
                    {
                        DisplayCachedItemsInstantly(folderItems);
                        // Refresh in background
                        _ = RefreshFolderCacheAsync(folderSource, selectedNode, cancellationToken);
                        return;
                    }

                    try
                    {
                        await LoadFolderPhotosAsync(folderSource, cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        // Expected - user switched sources
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected - user switched sources
                    }
                    catch (Exception ex)
                    {
                        LoadErrorMessage = $"Unable to load {folderSource.DisplayName}: {ex.Message}";
                    }
                    return;
                }

                // For built-in collection sources (Favorites and Recently Viewed)
                if (_selectedSource is not SourceItemViewModel { Provider: IPhotoProvider provider }) return;

                var sourceViewModel = (SourceItemViewModel)_selectedSource;

                string sourceKey = $"source:{_selectedSource.GetHashCode()}";
                    if (TryLoadFromCache(sourceKey, out var sourceItems) && !sourceItems.IsStale())
                    {
                        DisplayCachedItemsInstantly(sourceItems);
                        sourceViewModel.MarkLoadSucceeded();
                    // Refresh in background
                    _ = RefreshSourceCacheAsync(sourceKey, () => LoadSourcePhotosInternalAsync(provider, cancellationToken));
                    return;
                }

                try
                {
                    var photoItems = await provider.GetPhotoPathsAsync(cancellationToken);
                    if (cancellationToken.IsCancellationRequested) return;
                    await LoadPhotoItemsAsync(photoItems, sourceKey, cancellationToken);
                    sourceViewModel.MarkLoadSucceeded();
                }
                catch (TaskCanceledException)
                {
                    // Expected - user switched sources
                }
                catch (OperationCanceledException)
                {
                    // Expected - user switched sources
                }
                catch (Exception ex)
                {
                    sourceViewModel.MarkLoadFailed(ex);
                    OnPropertyChanged(nameof(LoadErrorTitle));
                    LoadErrorMessage = $"Unable to load {(_selectedSource as SourceItemViewModel)?.DisplayName ?? "this source"}: {ex.Message}";
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void ExecuteImportCommandAsync()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Choose photos to import",
                Filter = "Photos and videos|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.mp4;*.mov;*.avi;*.mkv;*.webm|All files (*.*)|*.*",
                Multiselect = true,
                CheckFileExists = true
            };
            if (openFileDialog.ShowDialog() != true) return;

            var selectionWindow = new ImportSelectionWindow(openFileDialog.FileNames)
            {
                Owner = System.Windows.Application.Current.Windows.OfType<MainWindow>().FirstOrDefault()
            };
            if (selectionWindow.ShowDialog() != true) return;
            var selectedFiles = selectionWindow.SelectedPaths.ToList();

            using var destinationDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Choose a destination folder for the imported items",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };
            if (destinationDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            var destination = destinationDialog.SelectedPath;
            var conflicts = selectedFiles.Count(path => File.Exists(Path.Combine(destination, Path.GetFileName(path))));
            if (conflicts > 0)
            {
                var result = System.Windows.MessageBox.Show(
                    $"{conflicts} item(s) already exist in the destination. Existing files will be skipped.",
                    "Duplicate files", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Information);
                if (result != System.Windows.MessageBoxResult.OK) return;
            }

            using var progressWindow = new ImportProgressWindow(selectedFiles.Count);
            progressWindow.Owner = System.Windows.Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            progressWindow.Show();

            ImportCopySummary summary = default;
            try
            {
                var progress = new Progress<ImportCopyProgress>(update =>
                    progressWindow.UpdateProgress(update.Index, update.FileName));
                summary = await new ImportCopyService().CopyAsync(
                    selectedFiles, destination, progress, progressWindow.CancellationToken);
            }
            finally
            {
                progressWindow.Close();
            }

            var canceled = summary.Canceled;
            var failedDetails = summary.FailedFiles.Count > 0
                ? $"\nFailed files: {string.Join(", ", summary.FailedFiles.Take(8))}{(summary.FailedFiles.Count > 8 ? ", ..." : string.Empty)}"
                : string.Empty;
            System.Windows.MessageBox.Show(
                $"Imported: {summary.Imported}\nSkipped duplicates: {summary.Skipped}\nFailed: {summary.Failed}{failedDetails}" + (canceled ? "\nThe import was canceled." : string.Empty),
                "Import complete", System.Windows.MessageBoxButton.OK,
                summary.Failed > 0 ? System.Windows.MessageBoxImage.Warning : System.Windows.MessageBoxImage.Information);

            if (summary.Imported > 0)
            {
                var existingFolder = Sources.OfType<FolderSourceViewModel>()
                    .FirstOrDefault(source => string.Equals(source.Provider.SourceName, destination, StringComparison.OrdinalIgnoreCase));
                if (existingFolder != null)
                    SelectedSource = existingFolder;
                else
                    AddLocalFolderSource(destination);
            }
        }

        private async Task LoadFolderPhotosAsync(FolderSourceViewModel folderSource, CancellationToken cancellationToken)
        {
            var selectedNode = folderSource.SelectedItem as PhotoViewer.Models.FolderNode;
            string cacheKey = selectedNode != null
                ? $"folder:{folderSource.GetHashCode()}:{selectedNode.FullName}"
                : $"folder:{folderSource.GetHashCode()}:root";

            // If root is selected (null), use provider's fast progressive enumeration
            // If specific folder is selected, use FolderNode's enumeration
            Func<IEnumerable<string>> enumeratorFactory = selectedNode == null
                ? () => folderSource.Provider.EnumeratePhotoPathsProgressively()
                : () => folderSource.GetSelectedPhotoPaths();

            await LoadPhotoItemsProgressivelyAsync(enumeratorFactory, cacheKey, cancellationToken);
        }

        private async Task LoadFolderPhotosInternalAsync(FolderSourceViewModel folderSource, PhotoViewer.Models.FolderNode? selectedNode, CancellationToken cancellationToken)
        {
            // Use provider's fast progressive enumeration when at root
            Func<IEnumerable<string>> enumeratorFactory = selectedNode == null
                ? () => folderSource.Provider.EnumeratePhotoPathsProgressively()
                : () => folderSource.GetSelectedPhotoPaths();

            await LoadPhotoItemsProgressivelyAsync(
                enumeratorFactory,
                $"folder:{folderSource.GetHashCode()}:{selectedNode?.FullName ?? "root"}", 
                cancellationToken,
                refreshCache: true);
        }

        private async Task LoadGalleryPhotosInternalAsync(CancellationToken cancellationToken)
        {
            var allProviders = Sources
                .OfType<FolderSourceViewModel>()
                .Select(s => (IPhotoProvider)s.Provider)
                .ToList();

            if (!allProviders.Any()) return;

            // Load providers progressively - don't wait for all to finish
            await LoadGalleryPhotosProgressivelyAsync(allProviders, cancellationToken);
        }

        /// <summary>
        /// Loads gallery photos by processing each provider sequentially.
        /// For local folders, uses progressive enumeration to stream results immediately.
        /// Caps display at MaxDisplayItems and stops enumeration early to avoid scanning all files.
        /// </summary>
        private async Task LoadGalleryPhotosProgressivelyAsync(List<IPhotoProvider> providers, CancellationToken cancellationToken)
        {
            // Clear current display
            if (_photos.Any())
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _photos.Clear());
            }

            var allItems = new List<PhotoItem>();
            var allViewModels = new List<PhotoItemViewModel>();
            var allFilePaths = new HashSet<string>();
            var displayCounter = new DisplayCounter { Count = 0 };
            const int maxCacheSize = int.MaxValue; // No cap

            // Clear gallery cache
            _photoCache.Remove("gallery");

            const int batchSize = 200;
            var uiUpdateQueue = new Queue<PhotoItemViewModel>();
            var flushTimer = System.Diagnostics.Stopwatch.StartNew();

            // Process providers SEQUENTIALLY
            var failedProviders = new List<string>();
            foreach (var provider in providers)
            {
                if (cancellationToken.IsCancellationRequested) break;
                if (displayCounter.Count >= maxCacheSize && allViewModels.Count >= maxCacheSize) break;

                try
                {
                    if (provider is LocalFolderProvider localProvider)
                        await ProcessLocalProviderSequentiallyAsync(localProvider, allItems, allViewModels, allFilePaths,
                            uiUpdateQueue, displayCounter, maxCacheSize, batchSize, cancellationToken, flushTimer);
                }
                catch (Exception ex)
                {
                    failedProviders.Add(provider.SourceName);
                    Debug.WriteLine($"Gallery provider failed: {provider.SourceName}: {ex.Message}");
                }
            }

            if (failedProviders.Count > 0)
                LoadWarningMessage = $"Some sources could not be loaded: {string.Join(", ", failedProviders)}";

            // Final flush
            await FlushUIQueueAsync(uiUpdateQueue, cancellationToken);

            // Remove duplicates from final cache
            var uniqueViewModels = allViewModels.GroupBy(vm => vm.Photo.FilePath)
                .Select(g => g.First())
                .ToList();

            if (!cancellationToken.IsCancellationRequested && uniqueViewModels.Any())
            {
                var entry = new PhotoCacheEntry
                {
                    Items = allItems,
                    ViewModels = uniqueViewModels,
                    FilePaths = allFilePaths,
                    CachedAt = DateTime.UtcNow
                };
                _photoCache["gallery"] = entry;
            }

            // Update total count
            TotalPhotoCount = allViewModels.Count >= maxCacheSize ? -1 : allViewModels.Count;
        }

        /// <summary>
        /// Processes a single local folder provider with throttled enumeration and UI updates.
        /// Stops early once enough items are cached.
        /// </summary>
        private async Task ProcessLocalProviderSequentiallyAsync(
            LocalFolderProvider provider,
            List<PhotoItem> allItems,
            List<PhotoItemViewModel> allViewModels,
            HashSet<string> allFilePaths,
            Queue<PhotoItemViewModel> uiUpdateQueue,
            DisplayCounter displayCounter,
            int maxCacheSize,
            int batchSize,
            CancellationToken cancellationToken,
            System.Diagnostics.Stopwatch flushTimer)
        {
            await Task.Run(() =>
            {
                var currentBatchPaths = new List<string>(batchSize);
                var localFlushTimer = System.Diagnostics.Stopwatch.StartNew();

                foreach (var filePath in provider.EnumeratePhotoPathsProgressively())
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    if (allViewModels.Count >= maxCacheSize) break;

                    currentBatchPaths.Add(filePath);

                    if (currentBatchPaths.Count >= batchSize)
                    {
                        var batchItems = ProcessBatch(currentBatchPaths, cancellationToken);
                        if (batchItems.Item3 > 0)
                        {
                            lock (allItems) { allItems.AddRange(batchItems.Item1); allViewModels.AddRange(batchItems.Item2); }
                            lock (allFilePaths) allFilePaths.UnionWith(currentBatchPaths);
                            lock (uiUpdateQueue)
                            {
                                foreach (var vm in batchItems.Item2)
                                {
                                    uiUpdateQueue.Enqueue(vm);
                                    displayCounter.Count++;
                                }
                            }
                        }

                        currentBatchPaths = new List<string>(batchSize);

                        if (localFlushTimer.ElapsedMilliseconds > 300 || uiUpdateQueue.Count >= batchSize)
                        {
                            FlushUIQueueFromBackground(uiUpdateQueue, cancellationToken);
                            localFlushTimer.Restart();
                        }
                    }
                }

                if (currentBatchPaths.Any() && !cancellationToken.IsCancellationRequested)
                {
                    var batchItems = ProcessBatch(currentBatchPaths, cancellationToken);
                    if (batchItems.Item3 > 0)
                    {
                        lock (allItems) { allItems.AddRange(batchItems.Item1); allViewModels.AddRange(batchItems.Item2); }
                        lock (allFilePaths) allFilePaths.UnionWith(currentBatchPaths);
                        lock (uiUpdateQueue)
                        {
                            foreach (var vm in batchItems.Item2)
                            {
                                uiUpdateQueue.Enqueue(vm);
                                displayCounter.Count++;
                            }
                        }
                    }
                }

                FlushUIQueueFromBackground(uiUpdateQueue, cancellationToken);
            }, cancellationToken);
        }

        private void FlushUIQueueFromBackground(Queue<PhotoItemViewModel> queue, CancellationToken cancellationToken)
        {
            List<PhotoItemViewModel> batch;
            lock (queue)
            {
                if (queue.Count == 0) return;
                batch = queue.ToList();
                queue.Clear();
            }
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                if (!cancellationToken.IsCancellationRequested) _photos.AddRange(batch);
            });
        }

        private async Task LoadSourcePhotosInternalAsync(IPhotoProvider provider, CancellationToken cancellationToken)
        {
            // Use progressive loading for all sources
            await LoadPhotoItemsProgressivelyFromProviderAsync(() => provider.GetPhotoPathsAsync(cancellationToken), $"source:{_selectedSource?.GetHashCode()}", cancellationToken, refreshCache: true);
        }

        /// <summary>
        /// Loads photo items progressively from an async provider.
        /// Waits for the provider to return, then streams to UI in batches.
        /// </summary>
        private async Task LoadPhotoItemsProgressivelyFromProviderAsync(Func<Task<IEnumerable<PhotoItem>>> providerFunc, string cacheKey, CancellationToken cancellationToken, bool refreshCache = false)
        {
            // Get all items from provider (may take time for large sources)
            var photoItems = await providerFunc();
            if (cancellationToken.IsCancellationRequested) return;

            // Then load progressively into UI
            await LoadPhotoItemsAsync(photoItems, cacheKey, cancellationToken, refreshCache);
        }

        private bool TryLoadFromCache(string cacheKey, out PhotoCacheEntry entry)
        {
            if (_photoCache.TryGetValue(cacheKey, out entry))
                return entry.Items.Count > 0;
            return false;
        }

        /// <summary>
        /// Displays cached items instantly from the cache. Uses pre-built ViewModels for zero-delay display.
        /// Replaces the entire collection with a single AddRange call, firing one CollectionChanged event
        /// so the UI renders all items at once with no population animation.
        /// </summary>
        private void DisplayCachedItemsInstantly(PhotoCacheEntry cachedEntry)
        {
            // Use pre-built viewmodels if available, otherwise fall back to building them
            var viewModels = cachedEntry.ViewModels;
            if (viewModels == null || viewModels.Count == 0)
            {
                viewModels = cachedEntry.Items.Select(item =>
                {
                    var vm = new PhotoItemViewModel(item);
                    vm.IsFavorite = _favoritesService.IsFavorite(item.FilePath);
                    return vm;
                }).ToList();
                cachedEntry.ViewModels = viewModels;
            }

            _photos.Clear();
            _photos.AddRange(viewModels);
        }

        private async Task RefreshSourceCacheAsync(string cacheKey, Func<Task> refreshFunc)
        {
            try
            {
                await refreshFunc();
            }
            catch { }
        }

        private async Task RefreshFolderCacheAsync(FolderSourceViewModel folderSource, PhotoViewer.Models.FolderNode? selectedNode, CancellationToken cancellationToken)
        {
            try
            {
                await LoadFolderPhotosInternalAsync(folderSource, selectedNode, cancellationToken);
            }
            catch { }
        }

        private async Task LoadPhotoItemsAsync(IEnumerable<PhotoItem> photoItems, string cacheKey, CancellationToken cancellationToken, bool refreshCache = false)
        {
            if (_photos.Any())
            {
                _photos.Clear();
            }

            var itemList = photoItems.ToList();

            // Build viewmodels and cache them
            var viewModels = new List<PhotoItemViewModel>(itemList.Count);
            const int viewModelBatchSize = 500;
            
            for (int i = 0; i < itemList.Count; i += viewModelBatchSize)
            {
                if (cancellationToken.IsCancellationRequested) return;
                
                var batch = itemList.Skip(i).Take(viewModelBatchSize);
                foreach (var item in batch)
                {
                    var vm = new PhotoItemViewModel(item)
                    {
                        IsFavorite = _favoritesService.IsFavorite(item.FilePath)
                    };
                    viewModels.Add(vm);
                }

                // Yield periodically during viewmodel construction
                if (i + viewModelBatchSize < itemList.Count)
                {
                    await Task.Yield();
                }
            }

            if (cancellationToken.IsCancellationRequested) return;

            // Update cache
            var entry = new PhotoCacheEntry
            {
                Items = itemList,
                ViewModels = viewModels,
                FilePaths = new HashSet<string>(itemList.Select(p => p.FilePath)),
                CachedAt = DateTime.UtcNow
            };
            _photoCache[cacheKey] = entry;

            // Add to UI collection in large batches using AddRange
            const int uiBatchSize = 500;
            for (int i = 0; i < viewModels.Count; i += uiBatchSize)
            {
                if (cancellationToken.IsCancellationRequested) return;

                var batch = viewModels.Skip(i).Take(uiBatchSize).ToList();
                
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    _photos.AddRange(batch);
                });

                // Yield between batches to keep UI responsive
                if (i + uiBatchSize < viewModels.Count)
                {
                    await Task.Yield();
                }
            }
        }

        /// <summary>
        /// Overload that accepts file paths (for folder tree selection).
        /// </summary>
        private async Task LoadPhotoItemsAsync(IEnumerable<string> filePaths, string cacheKey, CancellationToken cancellationToken, bool refreshCache = false)
        {
            var photoItems = filePaths.Select(fp =>
            {
                try
                {
                    return new PhotoItem(fp, Path.GetFileName(fp), File.GetCreationTime(fp), new FileInfo(fp).Length, 0, 0);
                }
                catch { return null; }
            }).Where(p => p != null).Select(p => p!);

            await LoadPhotoItemsAsync(photoItems, cacheKey, cancellationToken, refreshCache);
        }

        /// <summary>
        /// Loads photo items progressively by streaming from a lazy enumeration on a background thread.
        /// Caps displayed items to MaxDisplayItems to prevent UI lag with large sources.
        /// Stops enumeration after caching enough items to avoid scanning all 25,000+ files.
        /// Throttles UI updates to prevent flooding the dispatcher.
        /// </summary>
        private async Task LoadPhotoItemsProgressivelyAsync(Func<IEnumerable<string>> pathEnumeratorFactory, string cacheKey, CancellationToken cancellationToken, bool refreshCache = false)
        {
            if (_photos.Any())
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _photos.Clear());
            }

            // Clear cache for this key
            _photoCache.Remove(cacheKey);

            var allItems = new List<PhotoItem>();
            var allViewModels = new List<PhotoItemViewModel>();
            var allFilePaths = new HashSet<string>();

            const int batchSize = 200;
            int maxCacheSize = int.MaxValue; // No cap - load everything
            int displayedCount = 0;
            bool shouldStop = false;
            var uiUpdateQueue = new Queue<PhotoItemViewModel>();

            await Task.Run(async () =>
            {
                var currentBatchPaths = new List<string>(batchSize);
                var flushTimer = System.Diagnostics.Stopwatch.StartNew();

                foreach (var filePath in pathEnumeratorFactory())
                {
                    if (cancellationToken.IsCancellationRequested || shouldStop) break;

                    currentBatchPaths.Add(filePath);

                    if (currentBatchPaths.Count >= batchSize)
                    {
                        var batchItems = ProcessBatch(currentBatchPaths, cancellationToken);
                        if (batchItems.Item3 > 0)
                        {
                            lock (allItems)
                            {
                                allItems.AddRange(batchItems.Item1);
                                allViewModels.AddRange(batchItems.Item2);
                                allFilePaths.UnionWith(currentBatchPaths);
                            }

                            // Queue ALL items for UI display (no cap)
                            foreach (var vm in batchItems.Item2)
                            {
                                uiUpdateQueue.Enqueue(vm);
                                displayedCount++;
                            }

                            // Stop enumerating once we have enough cached
                            if (allViewModels.Count >= maxCacheSize)
                            {
                                shouldStop = true;
                            }
                        }

                        currentBatchPaths = new List<string>(batchSize);

                        // Flush queued UI updates every 200ms or when queue gets large
                        if (flushTimer.ElapsedMilliseconds > 200 || uiUpdateQueue.Count > 50)
                        {
                            await FlushUIQueueAsync(uiUpdateQueue, cancellationToken);
                            flushTimer.Restart();
                        }

                        // Small delay to yield CPU
                        await Task.Delay(5, cancellationToken);
                    }
                }

                // Process remaining items
                if (currentBatchPaths.Any() && !cancellationToken.IsCancellationRequested && !shouldStop)
                {
                    var batchItems = ProcessBatch(currentBatchPaths, cancellationToken);
                    if (batchItems.Item3 > 0)
                    {
                        lock (allItems)
                        {
                            allItems.AddRange(batchItems.Item1);
                            allViewModels.AddRange(batchItems.Item2);
                            allFilePaths.UnionWith(currentBatchPaths);
                        }

                        // Queue ALL remaining items
                        foreach (var vm in batchItems.Item2)
                        {
                            uiUpdateQueue.Enqueue(vm);
                            displayedCount++;
                        }
                    }
                }

                // Final flush
                await FlushUIQueueAsync(uiUpdateQueue, cancellationToken);
            }, cancellationToken);

            // Update total count (may be partial if we stopped early)
            TotalPhotoCount = allViewModels.Count >= maxCacheSize ? -1 : allViewModels.Count;

            // Update cache with accumulated data
            if (!cancellationToken.IsCancellationRequested && allViewModels.Count > 0)
            {
                var entry = new PhotoCacheEntry
                {
                    Items = allItems,
                    ViewModels = allViewModels,
                    FilePaths = allFilePaths,
                    CachedAt = DateTime.UtcNow
                };
                _photoCache[cacheKey] = entry;
            }
        }

        /// <summary>
        /// Flushes queued viewmodels to the UI collection in a single batch.
        /// </summary>
        private async Task FlushUIQueueAsync(Queue<PhotoItemViewModel> queue, CancellationToken cancellationToken)
        {
            if (queue.Count == 0) return;

            var batch = queue.ToList();
            queue.Clear();

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (cancellationToken.IsCancellationRequested) return;
                _photos.AddRange(batch);
            });
        }

        /// <summary>Processes a batch of file paths into PhotoItems and PhotoItemViewModels. Skips expensive file I/O.</summary>
        private (List<PhotoItem>, List<PhotoItemViewModel>, int) ProcessBatch(
            List<string> filePaths,
            CancellationToken cancellationToken)
        {
            var batchItems = new List<PhotoItem>(filePaths.Count);
            foreach (var fp in filePaths)
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    // Skip File.GetCreationTime and FileInfo.Length - these are expensive I/O ops
                    // Use filename as display name, 0 for dimensions/size (loaded later if needed)
                    var item = new PhotoItem(fp, Path.GetFileName(fp), DateTime.MinValue, 0, 0, 0);
                    batchItems.Add(item);
                }
                catch { /* Skip files that fail */ }
            }

            var batchViewModels = new List<PhotoItemViewModel>(batchItems.Count);
            foreach (var item in batchItems)
            {
                if (cancellationToken.IsCancellationRequested) break;

                // Skip favorites check during initial load - adds 1 I/O op per file
                var vm = new PhotoItemViewModel(item);
                batchViewModels.Add(vm);
            }

            return (batchItems, batchViewModels, batchItems.Count);
        }

        private async Task LoadGalleryPhotosAsync(CancellationToken cancellationToken)
        {
            var allProviders = new List<IPhotoProvider>();

            // Collect providers from folder sources
            foreach (var source in Sources.OfType<FolderSourceViewModel>())
            {
                allProviders.Add(source.Provider);
            }

            if (!allProviders.Any())
            {
                var gallerySource = CollectionSources.FirstOrDefault(s => s.DisplayName == "Gallery");
                if (gallerySource != null) gallerySource.PhotoCount = 0;
                return;
            }

            // Use progressive loading - don't wait for all providers to finish
            await LoadGalleryPhotosProgressivelyAsync(allProviders, cancellationToken);
        }

        private void ExecuteOpenSettingsCommand(object? parameter)
        {
            var settingsWindow = new SettingsWindow(this);
            settingsWindow.ShowDialog();
        }

        public void ApplyTheme()
        {
            SaveGalleryPreferences();
            ThemeManager.ApplyTheme(_selectedTheme);
        }

        private void SaveGalleryPreferences()
        {
            _settingsService.SaveSettings(new AppSettings
            {
                Theme = _selectedTheme,
                SortOption = _selectedSortOption,
                SortDirection = _selectedSortDirection,
                MediaType = _selectedMediaType,
                ThumbnailRowHeight = _thumbnailRowHeight,
                ConfirmDelete = _confirmDelete,
                MouseWheelZoom = _mouseWheelZoom,
                ShowFileLocation = _showFileLocation,
                AllowAnonymousDiagnostics = _allowAnonymousDiagnostics,
                PersonalizedExperiences = _personalizedExperiences,
                ReduceMotion = _reduceMotion,
                DefaultViewerZoom = _defaultViewerZoom
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private class DisplayCounter
        {
            public int Count;
        }
    }
}
