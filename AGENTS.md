# Application Agents (Components)

This document breaks down the application into its primary logical components, or "agents," and defines their responsibilities.

> **Note:** All UI components must follow the [GUI Design Standards](GUI_DESIGN_STANDARDS.md). Visual elements MUST include icons, images, and other graphical enhancements—no plain boring text.

---

## Theme Control Policy (STRICT)

**Theme is controlled in exactly ONE place: `ThemeManager.ApplyTheme()`.** This is the final say for all color decisions.

- **NO overrides allowed.** No XAML element, code-behind file, or style may hardcode colors (e.g., `#1a1a1a`, `White`, `#FF0000`) that bypass the theme system.
- **ALL colors MUST use `{DynamicResource}` references** to theme keys defined in `App.xaml` and managed by `ThemeManager`.
- **DWM (Desktop Window Manager) is the ultimate authority** for system theme detection. The `ThemeManager.IsSystemDarkMode()` method reads the Windows registry and is the single source of truth for system dark mode state.
- **No component may second-guess or override the theme.** This includes PhotoWindow, MainWindow, SettingsWindow, buttons, icons, navigation arrows, popups, tooltips, and all other visual elements.
- Violations of this policy will be rejected during code review.

See [GUI Design Standards](GUI_DESIGN_STANDARDS.md) for the complete theme compliance checklist.

---

### 1. Main Window Agent (Shell)

This is the main application window and central controller.

**Responsibilities:**
- Provide the main user interface for managing photo sources and viewing photos.
- Manage the lifecycle of all `PhotoWindow` instances.
- Act as the entry point for the application.
- Coordinate actions between the `SourcePersistenceService`, `LayoutService`, `SettingsService`, and various photo providers (e.g., `LocalFolderProvider`, `OneDriveProvider`).
- Load and display photos from the currently selected source.
- Default to showing the "Gallery" source (aggregating all sources) on startup.
- Provide theme settings (Light/Dark/System) persisted across sessions.
- Display photos in a justified gallery layout using `JustifiedWrapPanel` (variable widths based on actual aspect ratios, flush row edges).

---

### 2. Photo Window Agent (PhotoWindow)

This agent is a single window responsible for displaying one image.

**Responsibilities:**
- Display a single image from a given file path or URL.
- Handle user input for zooming (e.g., mouse wheel) and panning (e.g., mouse drag).
- Provide visual navigation controls (faint arrow buttons on left/right sides) to browse to the next/previous photo in the same folder.
- Support keyboard navigation (Left/Right arrow keys) for next/previous photo.
- Support deleting the current photo to Recycle Bin (Delete key).
- Store its own state: file path, window position (X, Y), size (Width, Height), and image zoom/pan state.
- Can be created, moved, resized, and closed independently.
- Fully theme-compliant — all colors use `{DynamicResource}` from the theme system. No hardcoded colors.

---

### 3. Layout Service Agent (LayoutService)

This is a non-visual agent that handles the logic for saving and loading workspace layouts.

**Responsibilities:**
- Collect state data from all active `PhotoWindow` instances.
- Serialize this collection of data into a JSON format.
- Write the JSON data to a specified file path.
- Read a JSON layout file and deserialize it into a data structure.
- Provide the deserialized data to the `Main Window Agent` to reconstruct the workspace.

---

### 4. Source Persistence Service Agent (SourcePersistenceService)

This is a non-visual agent that handles saving and loading the list of photo sources.

**Responsibilities:**
- Serialize the list of configured photo sources (e.g., Local Folders, OneDrive accounts, Google Drive accounts) into a JSON format.
- Save the JSON data to a file in the user's local application data folder.
- Read the JSON file and deserialize it into a list of source configurations.
- Provide the list of sources to the `Main Window Agent` on application startup.

---

### 5. Settings Service Agent (SettingsService)

This is a non-visual agent that handles saving and loading application settings.

**Responsibilities:**
- Persist application settings (e.g., theme preference) to a JSON file in `%LOCALAPPDATA%\PhotoViewer\`.
- Load saved settings on application startup.
- Provide the `ThemeManager` with the user's theme preference to apply Light, Dark, or System theme.

---

### 6. Folder Source Agent (FolderSourceViewModel + FolderNode)

This agent manages the expandable folder tree hierarchy for local folder sources.

**Responsibilities:**
- Build a tree of `FolderNode` objects from a root folder path, with lazy-loaded subfolders.
- Each `FolderNode` tracks its photo count, subfolder count, expansion state, and a reference to its parent `FolderSourceViewModel`.
- When a subfolder is selected, aggregate photo paths from that folder and all its descendants.
- When the root is selected, aggregate photo paths from the entire folder tree.
- Each node's `RootSource` property links it back to its owning `FolderSourceViewModel` for reliable selection handling.
- All color values use `{DynamicResource}` — no hardcoded theme colors.

---

### 7. Photo Cache Agent (in MainWindowViewModel)

This is a non-visual caching system for loaded photo items.

**Responsibilities:**
- Cache `PhotoItemViewModel` instances per source/subfolder using a `Dictionary<object, PhotoCacheEntry>`.
- On first visit: scan the source, build viewmodels, cache them, display with batching.
- On revisit: instantly display cached viewmodels via `RangeObservableCollection.AddRange()` (zero population animation).
- Kick off a background refresh after displaying cached content to catch file changes.
- Staleness detection: 5-minute timeout + sampling up to 50 files for existence (invalidates if >10% missing).
