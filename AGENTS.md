# Application Agents (Components)

This document breaks down the application into its primary logical components, or "agents," and defines their responsibilities.

> **Note:** All UI components must follow the [GUI Design Standards](GUI_DESIGN_STANDARDS.md). Visual elements MUST include icons, images, and other graphical enhancements—no plain boring text.

---

### 1. Main Window Agent (Shell)

This is the main application window and central controller.

**Responsibilities:**
- Provide the main user interface for managing photo sources and viewing photos.
- Manage the lifecycle of all `PhotoWindow` instances.
- Act as the entry point for the application.
- Coordinate actions between the `SourcePersistenceService`, `LayoutService`, and various photo providers (e.g., `LocalFolderProvider`, `OneDriveProvider`).
- Load and display photos from the currently selected source.

---

### 2. Photo Window Agent (PhotoWindow)

This agent is a single window responsible for displaying one image.

**Responsibilities:**
- Display a single image from a given file path or URL.
- Handle user input for zooming (e.g., mouse wheel) and panning (e.g., mouse drag).
- Provide navigation controls (faint arrow buttons on left/right sides) to browse to the next/previous photo in the same folder.
- Support keyboard navigation (Left/Right arrow keys) for next/previous photo.
- Store its own state: file path, window position (X, Y), size (Width, Height), and image zoom/pan state.
- Can be created, moved, resized, and closed independently.

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
