# Application Architecture

PhotoViewer uses WPF with an MVVM-oriented design. `MainWindowViewModel` coordinates sources, gallery state, open photo windows, persistence, and settings; views primarily bind to that state and handle WPF-specific events.

## Views and view models

- `MainWindow.xaml` / `MainWindowViewModel`: gallery shell, source navigation, commands, filtering, sorting, caching, and theme selection.
- `PhotoWindow.xaml` / `PhotoWindowViewModel`: image display, zoom, pan, navigation, deletion, and per-window state.
- `PhotoThumbnail.xaml` / `PhotoThumbnailViewModel`: gallery item presentation and favorite state.
- `SettingsWindow.xaml`: theme selection and settings interaction.
- `SourceItemViewModel`, `FolderSourceViewModel`, and source-specific view models: navigation entries and folder-tree behavior.

## Models and services

- `PhotoItem`: immutable photo metadata, including pixel dimensions and aspect ratio.
- `PhotoWindowState` and `WindowLayout`: JSON-serializable workspace models.
- `IPhotoProvider`: common asynchronous source contract.
- `LocalFolderProvider`, `OneDriveProvider`, `GoogleDriveProvider`, `iCloudProvider`, `FavoritesProvider`, and `RecentlyViewedProvider`: source implementations.
- `GalleryProvider`: aggregates configured providers and removes duplicate paths.
- `LayoutService`: manual and automatic layout serialization/restoration.
- `SourcePersistenceService`, `FavoritesService`, `HistoryService`, and `SettingsService`: local application-data persistence.
- `ThemeManager`: applies Light, Dark, or System resource dictionaries.
- `MainWindowSizeService` and `PhotoWindowSizeService`: persist window dimensions and placement.
- `JustifiedWrapPanel`: virtualized gallery panel that uses photo aspect ratios to create justified rows.

## Runtime flow

`App` creates one `MainWindowViewModel`. Direct launch displays `MainWindow`; file launch calls `OpenImage` and displays only a `PhotoWindow`. Selecting a source loads provider results asynchronously, displays them in batches, and caches completed results. A cache is considered stale after five minutes or when sampled files are missing.

Folder providers use `FileSystemWatcher` with a short debounce to refresh folder trees after directory changes. Theme changes update application resources through dynamic bindings. Closing the main window saves the current workspace layout under `%LOCALAPPDATA%\PhotoViewer`.
