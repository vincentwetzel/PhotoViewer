# Changelog

User-visible changes are recorded here. The project has not assigned release versions yet, so unreleased work is grouped under `[Unreleased]`.

## [Unreleased]

### Added

- Gallery, Favorites, and Recently Viewed collections.
- Local folder trees with photo counts and live folder updates.
- OneDrive, Google Drive, and local iCloud Photos providers.
- Justified aspect-ratio-aware gallery layout.
- Source caching with progressive loading and staleness checks.
- Light, Dark, and System themes with a custom title bar.
- Manual and automatic JSON workspace layout persistence.

### Changed

- Gallery items open on a single click.
- Sorting supports filename, creation date, and file size in both directions.
- Favorites use heart icons and Recently Viewed uses a clock/history icon.
- Folder-tree chevrons toggle expansion without changing the selected folder.

### Fixed

- Photo windows load their image correctly when opened from the gallery.
- Portrait and landscape thumbnails retain their actual proportions.
- Theme resources apply consistently to menus, scrollbars, backgrounds, text, and photo windows.
- Local source removal and favorite counts refresh correctly.
