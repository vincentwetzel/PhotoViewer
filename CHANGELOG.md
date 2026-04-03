# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog,
and this project adheres to Semantic Versioning.

## [Unreleased]

### Added
- **Gallery View**: "Gallery" source option that aggregates photos from all configured sources into one unified view.
- **Settings System**: Persistent application settings stored in `%LOCALAPPDATA%\PhotoViewer\settings.json`.
- **Theme Support**: Light, Dark, and System theme options accessible via the Settings cog in the title bar.
- **Custom Title Bar**: Full custom title bar with window title, Settings cog, minimize, maximize, and close buttons.
- **Favorites as Heart Icons**: Heart-shaped favorite/unfavorite buttons on gallery thumbnails and in the source navigation pane.
- **Sort Direction Toggle**: Ascending/Descending sort order option alongside the existing Sort By dropdown.
- **Settings Window**: Simple settings dialog with theme selector that applies changes in real-time.

### Changed
- **Default Launch View**: Application now opens directly to the Gallery tab instead of requiring manual selection.
- **Photo Window Navigation**: Left/right navigation arrows are now visible as faint overlay buttons on the photo window edges.
- **Gallery Opens on Single Click**: Selecting a photo in the gallery now opens it immediately (changed from double-click).
- **Remove Source Fix**: Fixed context menu binding so right-click → Remove now properly removes local folder sources.
- **PhotoWindow Initialization**: Fixed issue where PhotoWindow wasn't loading the image from its ViewModel, breaking navigation.

### Fixed
- Sort direction correctly applies to all sort options (File Name, Date Created, File Size).
- Context menu "Remove" source now correctly removes the selected source from the list.
- Photo thumbnails now open in PhotoWindow with a single click instead of requiring double-click.
- PhotoWindow correctly displays the loaded image (was showing gray screen due to missing binding).
- Favorite heart icons display fully without clipping on gallery thumbnails.
- Theme system properly applies dark mode to all UI elements (scrollbars, menus, sidebars, backgrounds, text).
- Settings cog icon renders without clipping in the custom title bar.
- MainWindow correctly loads the saved theme before XAML parsing begins.
- Gallery images now display with their true aspect ratios (portrait vs landscape) using the `JustifiedWrapPanel`.

### Gallery Improvements
- **Justified Gallery Layout**: Implemented `JustifiedWrapPanel` that arranges photos with variable widths based on actual aspect ratios, scaling each row to flush-fill the available width (like Google Photos).
- **Pixel Dimension Reading**: `LocalFolderProvider` now reads pixel dimensions from image files using `BitmapDecoder` for accurate aspect ratio calculation.
- **Fixed Row Height with Variable Widths**: Portrait photos appear narrower, landscape photos appear wider, creating a natural masonry-row layout.
- **Last Row Exception**: The final gallery row maintains target height and left-aligns without stretching when few images remain.

---

### Completed Features