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

## Integrations and local data

- Google Drive reads `client_secrets.json` from the working directory and stores OAuth tokens under the user's application data.
- OneDrive uses MSAL and a locally configured public-client ID.
- Sources, favorites, history, settings, window sizes, and the automatic layout are stored under `%LOCALAPPDATA%\PhotoViewer` (Google OAuth token storage follows Google's application-data path).
- Do not commit OAuth files, tokens, client IDs intended to remain private, or other credentials.

## Verification

Run `dotnet build PhotoViewer.sln` after code or XAML changes. Manually verify direct file launch, gallery source selection, layout save/load, theme switching, and cloud authentication when those areas change.
