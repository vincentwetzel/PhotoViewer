# Application Specification

This document provides detailed specifications for the Multi-Window Photo Viewer.

## 1. Functional Requirements

### 1.1. Main Window (Shell)
- **On Direct Launch**: When the application is started without any command-line arguments, it shall present a main gallery window.
- The gallery window will consist of two primary sections: a **Source Navigation Pane** on the left and a **Photo Grid** on the right.

#### 1.1.1. Source Navigation Pane
- This pane shall display a list of all configured photo sources (e.g., Local Folders, OneDrive, Google Drive, iCloud, Phone).
- The list will include user-configurable sources and built-in sources like "Recently Viewed".
- It must provide functionality for the user to add or remove sources.

#### 1.1.2. Photo Grid
- This area will display thumbnails of all images aggregated from the sources selected in the navigation pane.
- Clicking a thumbnail shall open the image in a new `PhotoWindow` (the "single image view").

#### 1.1.3. Layout Management
- The main gallery window shall contain controls (e.g., in a `File` menu) for layout management.
- **"Save Layout"**: Opens a "Save As" file dialog. It saves the state of all currently open `PhotoWindow` instances into a `.json` file.
- **"Load Layout"**: Opens a file dialog to select a `.json` layout file. It closes all open photo windows and restores the saved workspace by creating new `PhotoWindow` instances.

### 1.2. Single Image View (`PhotoWindow`)
- **On File Launch**: When the application is started with a file path as a command-line argument (e.g., from Windows Explorer), it shall directly open that image in a `PhotoWindow`, bypassing the main gallery window.
- Each `PhotoWindow` must display a single image.
- The window's title should reflect the file name of the image being displayed.
- The user must be able to freely move and resize the window using standard OS controls.
- **Zoom**: The user shall be able to zoom in and out of the image, centered on the mouse cursor's position, using the mouse wheel.
- **Pan**: When zoomed in, the user shall be able to pan the image by clicking and dragging with the primary mouse button.
- **Navigation**: The user shall be able to press the LEFT and RIGHT arrow keys to open the previous or next image file in the same directory, respectively. The new image should replace the image in the current `PhotoWindow`.
- **Delete Image**: The user shall be able to press the `DELETE` key to show a confirmation dialog. If confirmed, the application will move the currently displayed image file to the Recycle Bin and close the window.

## 2. Data Specification

The layout configuration will be saved in a JSON file. The structure of this file is defined as follows.

### 2.1. Root Object (`WindowLayout`)
...

## 2. Data Specification

The layout configuration will be saved in a JSON file. The structure of this file is defined as follows.

### 2.1. Root Object (`WindowLayout`)
- A top-level object containing a single property: `PhotoWindows`.
- `PhotoWindows`: An array of `PhotoWindowState` objects.

### 2.2. Window State Object (`PhotoWindowState`)
- An object representing the state of a single photo window. It contains the following properties:
  - `FilePath` (string): The absolute path to the image file.
  - `Top` (double): The Y-coordinate of the window's top edge.
  - `Left` (double): The X-coordinate of the window's left edge.
  - `Width` (double): The width of the window.
  - `Height` (double): The height of the window.
  - `ZoomLevel` (double): The zoom factor applied to the image (e.g., 1.0 for 100%).
  - `PanOffsetX` (double): The horizontal offset of the panned image.
  - `PanOffsetY` (double): The vertical offset of the panned image.

### 2.3. Example JSON Structure
```json
{
  "PhotoWindows": [
    {
      "FilePath": "C:\\Users\\User\\Pictures\\ref_01.png",
      "Top": 100,
      "Left": 150,
      "Width": 800,
      "Height": 600,
      "ZoomLevel": 1.5,
      "PanOffsetX": -50.0,
      "PanOffsetY": -25.0
    },
    {
      "FilePath": "C:\\Users\\User\\Pictures\\ref_02.jpg",
      "Top": 720,
      "Left": 150,
      "Width": 400,
      "Height": 300,
      "ZoomLevel": 1.0,
      "PanOffsetX": 0.0,
      "PanOffsetY": 0.0
    }
  ]
}
```

## 3. System Integration

### 3.1. Default Program Functionality
- The application must be able to be set as the default viewer for image files in Windows.
- **Command-Line Activation**: The application must handle being launched with a file path as a command-line argument. If launched this way, it should open the specified image in a new `PhotoWindow`.
- **File Associations**: The application should be associated with the following file extensions through its installer or registration process:
  - `.jpg`
  - `.jpeg`
  - `.png`
  - `.bmp`
```