# Application Architecture

PhotoViewer uses WPF with an MVVM-oriented design. `MainWindowViewModel` coordinates sources, gallery state, open photo windows, persistence, and settings; views primarily bind to that state and handle WPF-specific events.

## Editing persistence policy

Edits are session-scoped and non-destructive. The original file is never changed by viewer editing; the user must explicitly choose Save As to create an edited copy. The current release does not autosave drafts or write sidecar files. Image adjustments, filters, crop, geometry, annotation, and markup are composed by the editing service before encoding the copy. A future sidecar draft is only appropriate if it is recoverable, cancellable, clearly associated with the original path, and has explicit discard/restore controls.

## Import and media handling

The gallery import command selects local image/video files, then copies the
chosen items to a destination through `ImportCopyService`. It reports progress,
skips existing destination names, supports cancellation, and summarizes failed
copies. Device discovery and transfer are not implemented. Supported media
extensions are centralized in `MediaFileTypes`; video playback uses WPF media
support and can vary with installed Windows codecs.

## Views and view models

- `MainWindow.xaml` / `MainWindowViewModel`: gallery shell, source navigation, commands, filtering, sorting, caching, and theme selection.
- `PhotoWindow.xaml` / `PhotoWindowViewModel`: image display, zoom, pan, navigation, deletion, and per-window state.
- `PhotoThumbnail.xaml` / `PhotoThumbnailViewModel`: gallery item presentation and favorite state.
- `SettingsWindow.xaml`: scrollable Photos-style settings page for theme,
  gallery preferences, viewer behavior, privacy, and source guidance.
- `SourceItemViewModel` and `FolderSourceViewModel`: local navigation entries and folder-tree behavior.

## Models and services

- `PhotoItem`: immutable photo metadata, including pixel dimensions and aspect ratio.
- `PhotoWindowState` and `WindowLayout`: JSON-serializable workspace models.
- `IPhotoProvider`: common asynchronous source contract.
- `IProviderCapabilities` and `ProviderCapabilities`: explicit read/write,
  delete, move, edit, and offline behavior for honest provider-aware actions.
- `ImportCopyService`, `MediaFileTypes`, `PhotoQueryMatcher`,
  `PhotoSortDescriptor`, and `PhotoSelectionService`: local import and gallery
  behavior shared by the view model and tests.
- `LocalFolderProvider`, `FavoritesProvider`, and `RecentlyViewedProvider`: local source implementations.
- `GalleryProvider`: aggregates configured providers and removes duplicate paths.
- `LayoutService`: manual and automatic layout serialization/restoration.
- `SourcePersistenceService`, `FavoritesService`, and `HistoryService`: local application-data persistence.
- `ThemeManager`: applies Light, Dark, or System resource dictionaries.
- `SettingsService`: versioned JSON persistence for theme, gallery controls,
  and viewer/privacy preferences.
- `MainWindowSizeService` and `PhotoWindowSizeService`: persist window dimensions and placement.
- `JustifiedWrapPanel`: virtualized gallery panel that uses photo aspect ratios to create justified rows.

## Runtime flow

`App` creates one `MainWindowViewModel`. Direct launch displays `MainWindow`; file launch calls `OpenImage` and displays only a `PhotoWindow`. Selecting a local source loads results asynchronously, displays them in batches, and caches completed results. A cache is considered stale after five minutes or when sampled files are missing.

Folder providers use `FileSystemWatcher` with a short debounce to refresh folder trees after directory changes. Theme changes update application resources through dynamic bindings. Closing the main window saves the current workspace layout under `%LOCALAPPDATA%\PhotoViewer`.
