# Changelog

User-visible changes are recorded here. The project has not assigned release versions yet, so unreleased work is grouped under `[Unreleased]`.

## [Unreleased]

### Added

- Gallery, Favorites, and Recently Viewed collections.
- Persisted local folder trees with media counts and live folder updates.
- Justified aspect-ratio-aware gallery layout.
- Source caching with progressive loading and staleness checks.
- Light, Dark, and System themes with a custom title bar.
- Manual and automatic JSON workspace layout persistence.
- Multi-file local import with destination selection, duplicate skipping,
  progress, cancellation, and a completion summary.
- Image editing with undo/redo and Save As export, plus local video playback
  with codec fallback messaging.
- Gallery date grouping, media-type filtering, thumbnail density, selection,
  and bulk local actions.
- Windows file-association registration helper and MSIX manifest scaffolding.

### Changed

- Gallery items open on a single click.
- Sorting supports taken/created/modified dates, name, and file size in both directions.
- Favorites use heart icons and Recently Viewed uses a clock/history icon.
- Folder-tree chevrons toggle expansion without changing the selected folder.

### Fixed

- Photo windows load their image correctly when opened from the gallery.
- Portrait and landscape thumbnails retain their actual proportions.
- Theme resources apply consistently to menus, scrollbars, backgrounds, text, and photo windows.
- Local source removal and favorite counts refresh correctly.
