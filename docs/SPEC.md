# Application Specification

## Launch behavior

- With no command-line arguments, show `MainWindow` and select the Gallery collection.
- With a file path argument, create the central view model and open that file directly in a `PhotoWindow` without showing the gallery shell.

## Gallery shell

`MainWindow` contains a source navigation pane and a photo gallery. Built-in collections are Gallery, Favorites, and Recently Viewed. User sources are local folder trees, OneDrive, and Google Drive. iCloud Photos is discovered as a local folder provider; phone import is not implemented.

The gallery supports single-click opening, filename/date/size sorting in ascending or descending order, filtering, favorite toggling, photo counts, progressive loading, and cached source results. Local folder trees update through `FileSystemWatcher`.

## Photo window

Each `PhotoWindow` displays one image and supports:

- Mouse-wheel zoom centered on the pointer.
- Mouse-drag panning while zoomed.
- Previous/next navigation with overlay buttons or the Left/Right keys.
- Delete confirmation followed by moving the file to the Recycle Bin.
- Independent position, size, maximized state, zoom, and pan state.

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
