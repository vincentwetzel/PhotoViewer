# Multi-Window Photo Viewer

A custom photo viewer application built with C# and .NET that allows users to open multiple photos in separate, freely-movable windows. The primary feature is the ability to save and load the entire workspace layout—including which photos are open, their window positions, sizes, and zoom levels.

## The Problem

When working with multiple reference images, it's often necessary to arrange them on the screen in a specific way. Standard photo viewers require you to manually reopen, resize, and reposition each image every time you start a new session. This is tedious and time-consuming.

## The Solution

This application solves that problem by introducing layout persistence.

### Core Features

- **Multi-Window Interface**: Open each image in its own independent window.
- **Window Manipulation**: Freely move, resize, and zoom/pan each photo.
- **Save Layout**: Save the current arrangement of all open photo windows to a single configuration file (JSON).
- **Load Layout**: Restore a previously saved layout, reopening all photos and restoring their exact window position, size, and zoom level.

## Technology Stack

- **Language**: C#
- **Framework**: .NET (WPF for desktop UI)
- **Serialization**: JSON for layout files.

## Getting Started

To build and run this project, you will need the .NET SDK installed.

### Building and Running

```bash
dotnet build PhotoViewer.sln
dotnet run --project PhotoViewer.csproj
```

### Setting Up OneDrive Integration

To enable OneDrive integration, you must register the application in the Azure Portal:

1.  Go to the [Azure Portal](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade).
2.  Click **New registration**.
3.  Give your app a name (e.g., "PhotoViewer").
4.  Under 'Supported account types', select 'Accounts in any organizational directory... and personal Microsoft accounts...'.
5.  Under 'Redirect URI', select 'Public client/native (mobile & desktop)' and enter `http://localhost`.
6.  Click **Register**.
7.  Copy the **Application (client) ID** from the overview page.
8.  Open `OneDriveAuthenticationService.cs` and paste the Client ID into the `ClientId` constant.

### Setting Up Google Drive Integration

To enable Google Drive integration, you must create an OAuth 2.0 Client ID in the Google Cloud Console:

1.  Go to the [Google Cloud Console](https://console.cloud.google.com/apis/credentials).
2.  Click **Create Credentials** and select **OAuth client ID**.
3.  Select **Desktop app** as the application type.
4.  Give it a name (e.g., "PhotoViewer Desktop").
5.  Click **Create**.
6.  Click **Download JSON** to download the `client_secrets.json` file.
7.  Place the downloaded `client_secrets.json` file in the root of the `PhotoViewer` project directory.
8.  In Visual Studio, set the file's **Copy to Output Directory** property to **Copy if newer**.
