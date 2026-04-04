# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog,
and this project adheres to Semantic Versioning.

## [Unreleased]

### Added
- **Workspace Save/Load**: Open photo windows are automatically saved on exit and restored on next launch — including position, size, zoom level, pan state, and maximized state. Full multi-monitor support via virtual screen coordinates.
- **Manual Layout Save/Load**: **File → Save Layout...** and **File → Load Layout...** menu items let you export/import workspace layouts to/from JSON files.
- **Recently Viewed Clock Icon**: The "Recently Viewed" collection now uses a clock/history icon instead of a heart, making it visually distinct from Favorites.
- **Chevron Click Refinement**: Clicking directly on a folder's expand/collapse chevron toggles the folder state without changing the currently selected folder (the right panel remains unchanged). Clicking elsewhere on an expandable folder still selects it. Subfolder clicks always select normally.

### Changed
- **Left Panel Sizing Increased**: Default width 220→280, min 150→200, max 400→500. Icons enlarged (16→20, 18→22), font sizes increased (10→12, 11→13, 13→15), padding and spacing expanded throughout for a more comfortable reading experience.
- **Default Launch View**: Application now opens directly to the Gallery tab instead of requiring manual selection.
- **Photo Window Navigation**: Left/right navigation arrows are now visible as faint overlay buttons on the photo window edges.
- **Gallery Opens on Single Click**: Selecting a photo in the gallery now opens it immediately (changed from double-click).
- **Remove Source Fix**: Fixed context menu binding so right-click → Remove now properly removes local folder sources.
- **PhotoWindow Initialization**: Fixed issue where PhotoWindow wasn't loading the image from its ViewModel, breaking navigation.
- **PhotoWindow Title Bar**: Title bar dark mode now respects the app's theme setting (Light/Dark/System) instead of only following the system theme.

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