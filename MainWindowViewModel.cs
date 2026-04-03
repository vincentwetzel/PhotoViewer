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

namespace PhotoViewer.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public ICommand OpenImageCommand { get; }
        public ICommand SaveLayoutCommand { get; }
        public ICommand LoadLayoutCommand { get; }
        public ICommand AddLocalFolderCommand { get; }
        public ICommand AddOneDriveAccountCommand { get; }
        public ICommand ToggleFavoriteCommand { get; }
        public ICommand RemoveSourceCommand { get; }
        public ICommand AddGoogleDriveAccountCommand { get; }
        public ICommand OpenSettingsCommand { get; }

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
        private readonly LayoutService _layoutService;
        private readonly PhotoWindowSizeService _photoWindowSizeService;
        private readonly MainWindowSizeService _mainWindowSizeService;
        private CancellationTokenSource? _photoLoadingCts;
        private readonly OneDriveAuthenticationService _oneDriveAuthService;
        private readonly GoogleAuthenticationService _googleAuthService;
        private readonly FavoritesService _favoritesService;
        private readonly HistoryService _historyService;
        private readonly SourcePersistenceService _sourcePersistenceService;
        private readonly SettingsService _settingsService;

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
        /// <summary>User sources: Folders (as FolderSourceViewModel), OneDrive, Google Drive.</summary>
        public ObservableCollection<object> UserSources { get; } = new();

        /// <summary>The photo collection displayed in the gallery view. Uses RangeObservableCollection for instant batch updates.</summary>
        private readonly RangeObservableCollection<PhotoItemViewModel> _photos;
        public ICollectionView PhotosView { get; }

        private object? _selectedSource;
        public object? SelectedSource
        {
            get => _selectedSource;
            set
            {
                if (_selectedSource == value) return;
                _selectedSource = value;
                OnPropertyChanged(nameof(SelectedSource));
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

        public List<string> SortOptions { get; } = new List<string> { "File Name", "Date Created", "File Size" };

        private string _selectedSortOption = "File Name";
        public string SelectedSortOption
        {
            get => _selectedSortOption;
            set
            {
                if (_selectedSortOption == value) return;
                _selectedSortOption = value;
                OnPropertyChanged(nameof(SelectedSortOption));
                ApplySort();
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
            }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                PhotosView.Refresh();
            }
        }

        public MainWindowViewModel()
        {
            OpenImageCommand = new RelayCommand(ExecuteOpenImageCommand);
            SaveLayoutCommand = new RelayCommand(ExecuteSaveLayoutCommand);
            LoadLayoutCommand = new RelayCommand(ExecuteLoadLayoutCommand);
            AddLocalFolderCommand = new RelayCommand(ExecuteAddLocalFolderCommand);
            AddOneDriveAccountCommand = new RelayCommand(ExecuteAddOneDriveAccountCommandAsync);
            ToggleFavoriteCommand = new RelayCommand(ExecuteToggleFavoriteCommand);
            RemoveSourceCommand = new RelayCommand(ExecuteRemoveSourceCommand, CanExecuteRemoveSourceCommand);
            AddGoogleDriveAccountCommand = new RelayCommand(ExecuteAddGoogleDriveAccountCommandAsync);
            OpenSettingsCommand = new RelayCommand(ExecuteOpenSettingsCommand);

            _layoutService = new LayoutService();
            _photoWindowSizeService = new PhotoWindowSizeService();
            _mainWindowSizeService = new MainWindowSizeService();
            _oneDriveAuthService = new OneDriveAuthenticationService();
            _googleAuthService = new GoogleAuthenticationService();
            _favoritesService = new FavoritesService();
            _historyService = new HistoryService();
            _sourcePersistenceService = new SourcePersistenceService();
            _settingsService = new SettingsService();

            // Load saved settings
            var settings = _settingsService.LoadSettings();
            _selectedTheme = settings.Theme;

            _photos = new RangeObservableCollection<PhotoItemViewModel>();
            PhotosView = CollectionViewSource.GetDefaultView(_photos);
            PhotosView.Filter = FilterPhotos;
            ApplySort();
        }

        public async Task InitializeAsync()
        {
            try
            {
                AddDefaultSources();
                LoadPersistedSources();

                if (Sources.OfType<SourceItemViewModel>().Count(s => s.Provider is not FavoritesProvider and not RecentlyViewedProvider) == 0)
                {
                    await AddDefaultPicturesFolderAsync();
                }

                // Load counts for collection sources in the background
                _ = Task.Run(async () => await LoadCollectionSourceCountsAsync());
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Startup error: {ex.Message}\n\n{ex.StackTrace}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Computes and sets photo counts for all collection sources (Gallery, Favorites, Recently Viewed).
        /// </summary>
        private async Task LoadCollectionSourceCountsAsync()
        {
            foreach (var collection in CollectionSources)
            {
                try
                {
                    var photos = await collection.Provider.GetPhotoPathsAsync();
                    var count = photos.Count();
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => collection.PhotoCount = count);
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
        }

        private void ExecuteLoadLayoutCommand(object? parameter)
        {
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
                        }
                    }
                    else if (config.Type == "OneDrive")
                    {
                        if (string.IsNullOrWhiteSpace(config.Path)) continue;

                        // This part remains async for now due to silent authentication
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                var authResult = await _oneDriveAuthService.SignInSilentlyAsync(config.Path);
                                if (authResult != null)
                                {
                                    var oneDriveProvider = new OneDriveProvider(authResult);
                                    if (!Sources.OfType<OneDriveSourceViewModel>().Any(s => s.AccountId == authResult.Account.HomeAccountId.Identifier))
                                    {
                                        var newSource = new OneDriveSourceViewModel(oneDriveProvider)
                                        {
                                            DisplayName = config.DisplayName,
                                            AccountId = authResult.Account.HomeAccountId.Identifier
                                        };
                                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                        {
                                            Sources.Add(newSource);
                                            UserSources.Add(newSource);
                                        });
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error loading OneDrive source: {ex}");
                            }
                        });
                    }
                    else if (config.Type == "GoogleDrive")
                    {
                        if (string.IsNullOrWhiteSpace(config.Path)) continue;

                        // This part remains async for now due to silent authentication
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                var credential = await _googleAuthService.SignInInteractivelyAsync();
                                if (credential != null && credential.UserId == config.Path)
                                {
                                    var googleDriveProvider = new GoogleDriveProvider(credential);
                                    if (!Sources.OfType<GoogleDriveSourceViewModel>().Any(s => s.UserId == credential.UserId))
                                    {
                                        var newSource = new GoogleDriveSourceViewModel(googleDriveProvider)
                                        {
                                            DisplayName = config.DisplayName,
                                            UserId = credential.UserId
                                        };
                                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                        {
                                            Sources.Add(newSource);
                                            UserSources.Add(newSource);
                                        });
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error loading GoogleDrive source: {ex}");
                                // Ignore if authentication fails
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to load source config '{config.DisplayName}': {ex.Message}", "Source Load Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
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
                    else if (source is SourceItemViewModel svm && svm.Provider is OneDriveProvider)
                    {
                        var odsvm = (OneDriveSourceViewModel)source;
                        configsToSave.Add(new SourceConfig
                        {
                            Type = "OneDrive",
                            Path = odsvm.AccountId,
                            DisplayName = svm.DisplayName
                        });
                    }
                    else if (source is SourceItemViewModel svm2 && svm2.Provider is GoogleDriveProvider)
                    {
                        var gdsvm = (GoogleDriveSourceViewModel)source;
                        configsToSave.Add(new SourceConfig
                        {
                            Type = "GoogleDrive",
                            Path = gdsvm.UserId,
                            DisplayName = svm2.DisplayName
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
            if (string.IsNullOrEmpty(SearchText))
            {
                return true;
            }

            if (item is PhotoItemViewModel vm)
            {
                return vm.Photo.FileName.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private void ApplySort()
        {
            PhotosView.SortDescriptions.Clear();

            string propertyName = _selectedSortOption switch
            {
                "Date Created" => $"{nameof(PhotoItemViewModel.Photo)}.{nameof(PhotoItem.CreationDate)}",
                "File Size" => $"{nameof(PhotoItemViewModel.Photo)}.{nameof(PhotoItem.FileSizeInBytes)}",
                _ => $"{nameof(PhotoItemViewModel.Photo)}.{nameof(PhotoItem.FileName)}",
            };

            var direction = _selectedSortDirection == "Ascending" ? ListSortDirection.Ascending : ListSortDirection.Descending;
            PhotosView.SortDescriptions.Add(new SortDescription(propertyName, direction));
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
            return parameter is SourceItemViewModel { Provider: LocalFolderProvider or OneDriveProvider or GoogleDriveProvider }
                or FolderSourceViewModel;
        }

        private void ExecuteRemoveSourceCommand(object? parameter)
        {
            if (parameter is FolderSourceViewModel fsv)
            {
                fsv.Dispose();
            }

            if (parameter is not SourceItemViewModel and not FolderSourceViewModel) return;

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

                // Apply saved window size
                var savedSize = _photoWindowSizeService.LoadSize();
                photoWindow.Width = savedSize.Width;
                photoWindow.Height = savedSize.Height;

                _openPhotoWindows.Add(photoWindowViewModel);
                
                // Save window size when closed
                photoWindow.Closed += (sender, e) =>
                {
                    _openPhotoWindows.Remove(photoWindowViewModel);
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
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select a folder to add to the gallery",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var folderPath = dialog.SelectedPath;
                if (!Sources.Any(s => s is FolderSourceViewModel fsv && fsv.Provider.SourceName.Equals(folderPath, StringComparison.OrdinalIgnoreCase)))
                {
                    var provider = new LocalFolderProvider(folderPath);
                    var newSource = new FolderSourceViewModel(provider);
                    Sources.Add(newSource);
                    UserSources.Add(newSource);
                    SelectedSource = newSource;
                    PersistSources();
                }
            }
        }

        private async void ExecuteAddOneDriveAccountCommandAsync(object? parameter)
        {
            var authResult = await _oneDriveAuthService.SignInInteractivelyAsync();
            if (authResult != null)
            {
                var oneDriveProvider = new OneDriveProvider(authResult);
                var newSource = new OneDriveSourceViewModel(oneDriveProvider)
                {
                    DisplayName = $"OneDrive ({authResult.Account.Username})",
                    AccountId = authResult.Account.HomeAccountId.Identifier
                };
                Sources.Add(newSource);
                UserSources.Add(newSource);
                SelectedSource = newSource;
                PersistSources();
            }
            else
            {
                System.Windows.MessageBox.Show("Authentication failed or was canceled.", "Authentication Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async void ExecuteAddGoogleDriveAccountCommandAsync(object? parameter)
        {
            try
            {
                var credential = await _googleAuthService.SignInInteractivelyAsync();
                if (credential != null)
                {
                    var googleDriveProvider = new GoogleDriveProvider(credential);
                    var newSource = new GoogleDriveSourceViewModel(googleDriveProvider)
                    {
                        DisplayName = $"Google Drive ({credential.UserId})",
                        UserId = credential.UserId
                    };
                    Sources.Add(newSource);
                    UserSources.Add(newSource);
                    SelectedSource = newSource;
                    PersistSources();
                }
            }
            catch (Exception ex)
            { System.Windows.MessageBox.Show($"Google authentication failed: {ex.Message}", "Authentication Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error); }
        }

        private async void LoadPhotosForSelectedSourceAsync()
        {
            _photoLoadingCts?.Cancel();
            _photoLoadingCts = new CancellationTokenSource();
            var cancellationToken = _photoLoadingCts.Token;

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
                await LoadFolderPhotosAsync(folderSource, cancellationToken);
                return;
            }

            // For other source types (OneDrive, Google Drive, Favorites, Recently Viewed)
            if (_selectedSource is not SourceItemViewModel { Provider: IPhotoProvider provider }) return;

            string sourceKey = $"source:{_selectedSource.GetHashCode()}";
            if (TryLoadFromCache(sourceKey, out var sourceItems) && !sourceItems.IsStale())
            {
                DisplayCachedItemsInstantly(sourceItems);
                // Refresh in background
                _ = RefreshSourceCacheAsync(sourceKey, () => LoadSourcePhotosInternalAsync(provider, cancellationToken));
                return;
            }

            try
            {
                var photoItems = await provider.GetPhotoPathsAsync();
                if (cancellationToken.IsCancellationRequested) return;
                await LoadPhotoItemsAsync(photoItems, sourceKey, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Task was cancelled, which is expected.
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading photos: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async Task LoadFolderPhotosAsync(FolderSourceViewModel folderSource, CancellationToken cancellationToken)
        {
            var selectedNode = folderSource.SelectedItem as PhotoViewer.Models.FolderNode;
            string cacheKey = selectedNode != null
                ? $"folder:{folderSource.GetHashCode()}:{selectedNode.FullName}"
                : $"folder:{folderSource.GetHashCode()}:root";

            var photoPaths = await Task.Run(() => folderSource.GetSelectedPhotoPaths().ToList(), cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;

            await LoadPhotoItemsAsync(photoPaths, cacheKey, cancellationToken);
        }

        private async Task LoadFolderPhotosInternalAsync(FolderSourceViewModel folderSource, PhotoViewer.Models.FolderNode? selectedNode, CancellationToken cancellationToken)
        {
            var photoPaths = await Task.Run(() => folderSource.GetSelectedPhotoPaths().ToList(), cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;
            await LoadPhotoItemsAsync(photoPaths, $"folder:{folderSource.GetHashCode()}:{selectedNode?.FullName ?? "root"}", cancellationToken, refreshCache: true);
        }

        private async Task LoadGalleryPhotosInternalAsync(CancellationToken cancellationToken)
        {
            var allProviders = Sources
                .OfType<SourceItemViewModel>()
                .Where(s => s.DisplayName != "Gallery" && s.DisplayName != "Favorites" && s.DisplayName != "Recently Viewed")
                .Select(s => s.Provider)
                .ToList();

            if (!allProviders.Any()) return;

            var galleryProvider = new GalleryProvider(allProviders);
            var photoItems = await galleryProvider.GetPhotoPathsAsync();
            if (cancellationToken.IsCancellationRequested) return;
            await LoadPhotoItemsAsync(photoItems, "gallery", cancellationToken, refreshCache: true);
        }

        private async Task LoadSourcePhotosInternalAsync(IPhotoProvider provider, CancellationToken cancellationToken)
        {
            var photoItems = await provider.GetPhotoPathsAsync();
            if (cancellationToken.IsCancellationRequested) return;
            await LoadPhotoItemsAsync(photoItems, $"source:{_selectedSource?.GetHashCode()}", cancellationToken, refreshCache: true);
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
            foreach (var item in itemList)
            {
                if (cancellationToken.IsCancellationRequested) break;
                var vm = new PhotoItemViewModel(item)
                {
                    IsFavorite = _favoritesService.IsFavorite(item.FilePath)
                };
                viewModels.Add(vm);
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

            await Task.Run(async () =>
            {
                const int batchSize = 50;
                var batch = new List<PhotoItemViewModel>(batchSize);

                foreach (var vm in viewModels)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    batch.Add(vm);

                    if (batch.Count == batchSize)
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (cancellationToken.IsCancellationRequested) return;
                            foreach (var photoVm in batch)
                                _photos.Add(photoVm);
                        });
                        batch.Clear();
                    }
                }

                if (batch.Any() && !cancellationToken.IsCancellationRequested)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        foreach (var photoVm in batch)
                            _photos.Add(photoVm);
                    });
                }
            }, cancellationToken);
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

        private async Task LoadGalleryPhotosAsync(CancellationToken cancellationToken)
        {
            var allProviders = new List<IPhotoProvider>();

            // Collect providers from folder sources
            foreach (var source in Sources.OfType<FolderSourceViewModel>())
            {
                allProviders.Add(source.Provider);
            }

            // Collect providers from cloud sources (OneDrive, Google Drive, etc.)
            foreach (var source in Sources.OfType<SourceItemViewModel>())
            {
                if (source.DisplayName != "Gallery" && source.DisplayName != "Favorites" && source.DisplayName != "Recently Viewed")
                {
                    allProviders.Add(source.Provider);
                }
            }

            if (!allProviders.Any())
            {
                var gallerySource = CollectionSources.FirstOrDefault(s => s.DisplayName == "Gallery");
                if (gallerySource != null) gallerySource.PhotoCount = 0;
                return;
            }

            var galleryProvider = new GalleryProvider(allProviders);
            var photoItems = await galleryProvider.GetPhotoPathsAsync();
            if (cancellationToken.IsCancellationRequested) return;

            // Update Gallery count
            var galleryItem = CollectionSources.FirstOrDefault(s => s.DisplayName == "Gallery");
            if (galleryItem != null)
                galleryItem.PhotoCount = photoItems.Count();

            await LoadPhotoItemsAsync(photoItems, "gallery", cancellationToken);
        }

        private void ExecuteOpenSettingsCommand(object? parameter)
        {
            var settingsWindow = new SettingsWindow(this);
            settingsWindow.ShowDialog();
        }

        public void ApplyTheme()
        {
            _settingsService.SaveSettings(new AppSettings { Theme = _selectedTheme });
            ThemeManager.ApplyTheme(_selectedTheme);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}