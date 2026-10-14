# Application Specification

## Launch behavior

- With no command-line arguments, show `MainWindow` and select the Gallery collection.
- With a file path argument, create the central view model and open that file directly in a `PhotoWindow` without showing the gallery shell.

## Gallery shell

`MainWindow` contains a source navigation pane and a photo/video gallery. Built-in collections are Gallery, Favorites, and Recently Viewed. User sources are local folder trees. Import copies selected supported media files into a chosen local folder with duplicate detection, progress reporting, and cancellation. Phone/MTP import is not implemented.

The gallery supports single-click opening, Date taken/created/modified, Name,
and file-size sorting in ascending or descending order, composable
filename/path/date and media-type filtering, selection mode with bulk favorite
toggling and local file actions, thumbnail density control, favorite toggling,
photo/video counts, progressive loading, and cached source results. Date-based
gallery grouping follows the selected date sort. Local folder trees update
through `FileSystemWatcher`.

## Photo window

Each `PhotoWindow` displays one image and supports:

- Mouse-wheel zoom centered on the pointer.
- Mouse-drag panning while zoomed.
- Previous/next navigation with overlay buttons or the Left/Right keys.
- Delete confirmation followed by moving the file to the Recycle Bin.
- Independent position, size, maximized state, zoom, and pan state.
- A dark presentation surface, fit/actual-size/reset zoom controls, keyboard
  shortcuts, and an optional file information panel.
- Non-destructive session edits: rotate, flip, straighten, preset crop,
  adjustments, filters, annotations, markup, undo/redo, reset, and cancel.
- Save As export to JPEG, PNG, or BMP. The original stays unchanged by editing;
  edits are not saved as drafts when the window closes.
- Video playback controls for supported local media, including play/pause,
  seeking, and volume. Codec availability depends on Windows.

## Settings

Settings are stored as versioned JSON under `%LOCALAPPDATA%\\PhotoViewer`. The
settings page applies theme, gallery sort/filter/density, delete confirmation,
mouse-wheel zoom, and file-location visibility changes immediately.

## Layout persistence

Manual File → Save Layout and File → Load Layout commands read and write JSON. The application also saves the current open-window layout on close and restores it on the next launch.

The root JSON object is:

```json
{
  "PhotoWindows": [
    {
      "FilePath": "C:\\Pictures\\photo.jpg",
      "Top": 100,
      "Left": 150,
      "Width": 800,
      "Height": 600,
      "ZoomLevel": 1.5,
      "PanOffsetX": -50,
      "PanOffsetY": -25,
      "IsMaximized": false
    }
  ]
}
```

`PhotoWindowState` stores `FilePath`, `Top`, `Left`, `Width`, `Height`, `ZoomLevel`, `PanOffsetX`, `PanOffsetY`, and `IsMaximized`.

## Source configuration

Persisted user sources are JSON objects with `Type`, `Path`, and `DisplayName`. Providers implement `IPhotoProvider`, which returns `PhotoItem` values containing the path, name, creation date, byte size, pixel dimensions, and derived aspect ratio.
