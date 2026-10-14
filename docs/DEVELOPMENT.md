# Development Guide

## Environment

Development and execution require Windows, the .NET 8 SDK, and the Windows desktop targeting pack. The project uses WPF and Windows Forms on `net8.0-windows`.

## Build and run

From the repository root:

```powershell
dotnet restore PhotoViewer.sln
dotnet build PhotoViewer.sln
dotnet run --project src/PhotoViewer/PhotoViewer.csproj
```

Build output is generated in `bin/` and `obj/` and is ignored by Git.

## Source layout

Use [Project Structure](PROJECT_STRUCTURE.md) to place new files. Keep UI code in views and view models, photo discovery behind `IPhotoProvider`, and persistence in the existing services.

## Local data

Sources, favorites, history, settings, window sizes, and the automatic layout are stored under `%LOCALAPPDATA%\PhotoViewer`.

## Verification

Run `dotnet build PhotoViewer.sln` after code or XAML changes. Run the Windows test project with `dotnet test tests/PhotoViewer.Tests/PhotoViewer.Tests.csproj` for service and model behavior. Use [Manual Test Matrix](MANUAL_TEST_MATRIX.md) for UI workflows, including import, editing, playback, selection, and window persistence.
