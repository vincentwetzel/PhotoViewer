# Application Architecture

This document outlines the architectural design of the Multi-Window Photo Viewer application. The project will be built using C# and WPF, following the Model-View-ViewModel (MVVM) design pattern to ensure a clean separation of concerns.

> **Note:** All View layer components must follow the [GUI Design Standards](GUI_DESIGN_STANDARDS.md). Visual elements MUST include icons, images, and other graphical enhancements—no plain boring text.

## Core Components

The application is divided into three main layers:

### 1. View Layer (UI)

- **`MainWindow.xaml`**: The main application shell. It is a simple window containing the primary controls: "Open Image(s)", "Save Layout", and "Load Layout". It acts as the parent container but has minimal business logic.
- **`MainWindow.xaml`**: The main application shell, which serves as the **Gallery View**. It is displayed when the app is launched directly. It contains the source navigation and photo thumbnail grid.
- **`PhotoWindow.xaml`**: A reusable window that serves as the **Single Image View**. It is used to display one photo. It can be launched either by clicking a thumbnail in the gallery or by opening an image file directly from the OS. Features include:
  - Zoom (mouse wheel) and pan (mouse drag) functionality
  - Visual navigation arrows (left/right) for browsing photos in the same folder
  - Keyboard navigation (Left/Right arrow keys) for next/previous photo
  - Delete key support to move files to Recycle Bin

### 2. ViewModel Layer (Application Logic)

- **`MainWindowViewModel.cs`**: The central orchestrator for the entire application. It is instantiated at startup and persists for the application's lifetime.
  - It manages the state for the Gallery View (photo sources, thumbnail collections).
  - It is responsible for creating and tracking all `PhotoWindow` instances, regardless of whether they are opened from the gallery or from a file launch.
  - It coordinates with the `LayoutService` to save and load the layout of all open `PhotoWindow`s.
- **`PhotoWindowViewModel.cs`**: The state and logic for a single `PhotoWindow`. It holds the image source, zoom level, pan offsets, and window dimensions. It exposes commands and properties that the `PhotoWindow.xaml` binds to.

### 3. Model & Service Layer (Data and Business Rules)

- **Models (`WindowLayout.cs`, `PhotoWindowState.cs`)**: Plain C# objects (POCOs) that define the data structure for our saved layouts. These models are designed for easy serialization to and from JSON.
  - `WindowLayout`: Represents the entire saved workspace, containing a list of individual photo window states.
  - `PhotoWindowState`: Represents the state of a single photo window (file path, position, size, zoom, etc.).

- **`LayoutService.cs`**: A dedicated service responsible for all file I/O related to layouts.
  - **`SaveLayout(layout, filePath)`**: Serializes a `WindowLayout` model to a JSON file.
  - **`LoadLayout(filePath)`**: Deserializes a JSON file back into a `WindowLayout` model.
- **`HistoryService.cs`**: A service to track and persist a list of recently opened image files. It will provide the data for the "Recently Viewed" source in the gallery.

## Data Flow

- **Direct Launch**: `App.xaml.cs` starts the app, creates the `MainWindowViewModel`, and shows the `MainWindow` (Gallery). When a user clicks a thumbnail, the `MainWindowViewModel` is commanded to open a new `PhotoWindow`.
- **File Launch**: `App.xaml.cs` detects a command-line argument. It still creates the `MainWindowViewModel` to act as the central controller, but instead of showing the `MainWindow`, it directly commands the ViewModel to open a `PhotoWindow` for the specified file path. The `MainWindow` is not shown to the user in this case.
- **Saving a Layout**: The `MainWindowViewModel` gathers the state from each active `PhotoWindowViewModel`, assembles a `WindowLayout` model, and passes it to the `LayoutService` to be written to disk.
- **Loading a Layout**: The `MainWindowViewModel` uses the `LayoutService` to read a file into a `WindowLayout` model. It then iterates through the states in the model, creating and showing a new `PhotoWindow` for each one, configured with the specified properties.