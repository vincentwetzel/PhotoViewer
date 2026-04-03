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
        private CancellationTokenSource? _photoLoadingCts;
        private readonly OneDriveAuthenticationService _oneDriveAuthService;
        private readonly GoogleAuthenticationService _googleAuthService;
        private readonly FavoritesService _favoritesService;
        private readonly HistoryService _historyService;
        private readonly SourcePersistenceService _sourcePersistenceService;
        private readonly SettingsService _settingsService;

        public ObservableCollection<SourceItemViewModel> Sources { get; } = new();

        private readonly ObservableCollection<PhotoItemViewModel> _photos;
        public ICollectionView PhotosView { get; }

        private SourceItemViewModel? _selectedSource;
        public SourceItemViewModel? SelectedSource
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
            _oneDriveAuthService = new OneDriveAuthenticationService();
            _googleAuthService = new GoogleAuthenticationService();
            _favoritesService = new FavoritesService();
            _historyService = new HistoryService();
            _sourcePersistenceService = new SourcePersistenceService();
            _settingsService = new SettingsService();

            // Load saved settings
            var settings = _settingsService.LoadSettings();
            _selectedTheme = settings.Theme;

            _photos = new ObservableCollection<PhotoItemViewModel>();
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

                if (Sources.Count(s => s.Provider is not FavoritesProvider and not RecentlyViewedProvider) == 0)
                {
                    await AddDefaultPicturesFolderAsync();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Startup error: {ex.Message}\n\n{ex.StackTrace}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddDefaultSources()
        {
            // Add Gallery as the first source
            var galleryProvider = new GalleryProvider(Enumerable.Empty<IPhotoProvider>());
            var gallerySource = new SourceItemViewModel(galleryProvider) { DisplayName = "Gallery" };
            Sources.Add(gallerySource);

            var favoritesProvider = new FavoritesProvider(_favoritesService);
            var favoritesSource = new SourceItemViewModel(favoritesProvider) { DisplayName = "Favorites" };
            Sources.Add(favoritesSource);

            var recentlyViewedProvider = new RecentlyViewedProvider(_historyService);
            var recentlyViewedSource = new SourceItemViewModel(recentlyViewedProvider) { DisplayName = "Recently Viewed" };
            Sources.Add(recentlyViewedSource);
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
                            if (!Sources.Any(s => s.Provider is LocalFolderProvider lfp && lfp.SourceName == provider.SourceName))
                            {
                                var newSource = new SourceItemViewModel(provider) { DisplayName = "Pictures" };
                                Sources.Add(newSource);
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
                        if (!Sources.Any(s => s.Provider is LocalFolderProvider lfp && lfp.SourceName == provider.SourceName))
                        {
                            var newSource = new SourceItemViewModel(provider) { DisplayName = config.DisplayName ?? Path.GetFileName(config.Path) };
                            Sources.Add(newSource);
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
                                    if (!Sources.Any(s => s.Provider is OneDriveProvider odp && ((OneDriveSourceViewModel)s).AccountId == authResult.Account.HomeAccountId.Identifier))
                                    {
                                        var newSource = new OneDriveSourceViewModel(oneDriveProvider)
                                        {
                                            DisplayName = config.DisplayName,
                                            AccountId = authResult.Account.HomeAccountId.Identifier
                                        };
                                        System.Windows.Application.Current.Dispatcher.Invoke(() => Sources.Add(newSource));
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
                                    if (!Sources.Any(s => s.Provider is GoogleDriveProvider gdp && ((GoogleDriveSourceViewModel)s).UserId == credential.UserId))
                                    {
                                        var newSource = new GoogleDriveSourceViewModel(googleDriveProvider)
                                        {
                                            DisplayName = config.DisplayName,
                                            UserId = credential.UserId
                                        };
                                        System.Windows.Application.Current.Dispatcher.Invoke(() => Sources.Add(newSource));
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
                    if (source.Provider is LocalFolderProvider lfp)
                    {
                        configsToSave.Add(new SourceConfig
                        {
                            Type = "LocalFolder",
                            Path = lfp.SourceName,
                            DisplayName = source.DisplayName
                        });
                    }
                    else if (source.Provider is OneDriveProvider)
                    {
                        var odsvm = (OneDriveSourceViewModel)source;
                        configsToSave.Add(new SourceConfig
                        {
                            Type = "OneDrive",
                            Path = odsvm.AccountId,
                            DisplayName = source.DisplayName
                        });
                    }
                    else if (source.Provider is GoogleDriveProvider)
                    {
                        var gdsvm = (GoogleDriveSourceViewModel)source;
                        configsToSave.Add(new SourceConfig
                        {
                            Type = "GoogleDrive",
                            Path = gdsvm.UserId,
                            DisplayName = source.DisplayName
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
                _favoritesService.RemoveFavorite(vm.Photo.FilePath); // Corrected line
        }

        private bool CanExecuteRemoveSourceCommand(object? parameter)
        {
            if (parameter is not SourceItemViewModel vm) return false;
            return vm.Provider is LocalFolderProvider or OneDriveProvider or GoogleDriveProvider;
        }

        private void ExecuteRemoveSourceCommand(object? parameter)
        {
            if (parameter is not SourceItemViewModel sourceToRemove) return;

            if (SelectedSource == sourceToRemove)
            {
                SelectedSource = Sources.FirstOrDefault(s => s != sourceToRemove);
            }

            Sources.Remove(sourceToRemove);

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

                _openPhotoWindows.Add(photoWindowViewModel);
                photoWindow.Closed += (sender, e) => _openPhotoWindows.Remove(photoWindowViewModel);

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
                if (!Sources.Any(s => s.Provider is LocalFolderProvider lfp && lfp.SourceName.Equals(folderPath, StringComparison.OrdinalIgnoreCase)))
                {
                    var provider = new LocalFolderProvider(folderPath);
                    var newSource = new SourceItemViewModel(provider) { DisplayName = Path.GetFileName(folderPath) };
                    Sources.Add(newSource);
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

            if (_photos.Any())
            {
                _photos.Clear();
            }

            if (_selectedSource?.Provider is not IPhotoProvider provider) return;

            // If Gallery is selected, aggregate photos from all sources
            if (_selectedSource.DisplayName == "Gallery")
            {
                await LoadGalleryPhotosAsync(cancellationToken);
                return;
            }

            try
            {
                var photoItems = await provider.GetPhotoPathsAsync();
                if (cancellationToken.IsCancellationRequested) return;

                await Task.Run(async () =>
                {
                    const int batchSize = 50;
                    var batch = new List<PhotoItemViewModel>(batchSize);

                    foreach (var item in photoItems)
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        var vm = new PhotoItemViewModel(item)
                        {
                            IsFavorite = _favoritesService.IsFavorite(item.FilePath)
                        };
                        batch.Add(vm);

                        if (batch.Count == batchSize)
                        {
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                if (cancellationToken.IsCancellationRequested) return;
                                foreach (var photoVm in batch)
                                {
                                    _photos.Add(photoVm);
                                }
                            });
                            batch.Clear();
                        }
                    }

                    if (batch.Any() && !cancellationToken.IsCancellationRequested)
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (cancellationToken.IsCancellationRequested) return;
                            foreach (var photoVm in batch)
                            {
                                _photos.Add(photoVm);
                            }
                        });
                    }
                }, cancellationToken);
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

        private async Task LoadGalleryPhotosAsync(CancellationToken cancellationToken)
        {
            var allProviders = Sources
                .Where(s => s.DisplayName != "Gallery" && s.DisplayName != "Favorites" && s.DisplayName != "Recently Viewed")
                .Select(s => s.Provider)
                .ToList();

            if (!allProviders.Any()) return;

            var galleryProvider = new GalleryProvider(allProviders);
            var photoItems = await galleryProvider.GetPhotoPathsAsync();
            
            if (cancellationToken.IsCancellationRequested) return;

            await Task.Run(async () =>
            {
                const int batchSize = 50;
                var batch = new List<PhotoItemViewModel>(batchSize);

                foreach (var item in photoItems)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    var vm = new PhotoItemViewModel(item)
                    {
                        IsFavorite = _favoritesService.IsFavorite(item.FilePath)
                    };
                    batch.Add(vm);

                    if (batch.Count == batchSize)
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (cancellationToken.IsCancellationRequested) return;
                            foreach (var photoVm in batch)
                            {
                                _photos.Add(photoVm);
                            }
                        });
                        batch.Clear();
                    }
                }

                if (batch.Any() && !cancellationToken.IsCancellationRequested)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        foreach (var photoVm in batch)
                        {
                            _photos.Add(photoVm);
                        }
                    });
                }
            }, cancellationToken);
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