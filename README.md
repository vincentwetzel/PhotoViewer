# PhotoViewer

PhotoViewer is a Windows WPF application for browsing local and connected photo sources, opening images in independent windows, and preserving a multi-window workspace.

## Features

- Gallery, Favorites, and Recently Viewed collections.
- Local folder sources with expandable folder trees and live folder-change detection.
- OneDrive and Google Drive sources with OAuth authentication.
- iCloud Photos discovery through the local `Pictures\iCloud Photos` folder.
- Justified, aspect-ratio-aware thumbnail layout with progressive loading, caching, sorting, and filtering.
- Single-click image opening, zoom and pan, previous/next navigation, and Delete-to-Recycle-Bin.
- JSON layout save/load and automatic workspace restore on exit/startup.
- Light, Dark, and System themes.

Phone import and iCloud sync menu entries are present in the UI, but phone import is not currently implemented and the iCloud provider reads the local iCloud Photos directory.

## Requirements

- Windows with the .NET 8 SDK and Windows desktop targeting pack.
- A configured Google OAuth desktop client for Google Drive.
- An Azure public-client application ID for OneDrive.

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

## Optional integrations

For Google Drive, create a desktop OAuth client in the [Google Cloud Console](https://console.cloud.google.com/apis/credentials), download `client_secrets.json`, and place it in the application working directory during local development. The file must not be committed.

For OneDrive, register a public client in the [Microsoft Azure Portal](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade) and configure the client ID in `OneDriveAuthenticationService.cs` for local development. Never commit credentials or certificates.

## Documentation

See [`docs/`](docs/) for the architecture, application specification, development guide, contribution workflow, UI standards, project structure, changelog, and TODO list.
