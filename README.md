# PhotoViewer

PhotoViewer is a Windows WPF application for browsing local media folders, opening items in independent windows, and preserving a multi-window workspace.

## Features

- Gallery, Favorites, and Recently Viewed collections.
- Local folder sources with expandable folder trees and live folder-change detection.
- Justified, aspect-ratio-aware photo and video gallery with date grouping, search, media filters, sorting, selection, and bulk actions.
- Import photos and videos into a chosen local folder with duplicate skipping, progress, and cancellation.
- Image and video viewing with zoom, pan, playback controls, previous/next navigation, and Delete-to-Recycle-Bin.
- Non-destructive image edits with crop, rotate, adjustments, filters, markup, undo/redo, and Save As.
- JSON layout save/load and automatic workspace restore on exit/startup.
- Light, Dark, and System themes.

The source model is intentionally local-folder-only. Phone/MTP discovery and Windows share-target integration are not implemented. Video playback depends on the codecs available to Windows.

## Requirements

- Windows with the .NET 8 SDK and Windows desktop targeting pack.

## Build and run

```powershell
dotnet restore PhotoViewer.sln
dotnet build PhotoViewer.sln
dotnet run --project src/PhotoViewer/PhotoViewer.csproj
```

To open an image directly:

```powershell
dotnet run --project src/PhotoViewer/PhotoViewer.csproj -- "C:\Pictures\photo.jpg"
```

## Documentation

See [`docs/`](docs/) for the architecture, application specification, development guide, manual test matrix, Windows integration notes, release checklist, contribution workflow, UI standards, project structure, changelog, and TODO list.
