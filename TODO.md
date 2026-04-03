# Project To-Do List

This To-Do list outlines the development plan for adding a unified gallery and multi-source support to the PhotoViewer application.

## Milestone 1: Gallery UI and Local Folders
- [X] Redesign `MainWindow.xaml` to have a two-panel layout: a side navigation bar for sources and a main grid for photo thumbnails.
- [X] Create a `PhotoThumbnail` user control for the gallery grid.
- [X] Implement the logic to add and list local folders as photo sources.
- [X] Implement a `LocalFolderProvider` service that scans specified folders for images (`.jpg`, `.png`, etc.).
- [X] Asynchronously load and display image thumbnails in the main grid from the selected local folders.
- [X] Clicking a thumbnail should open the image in the existing `PhotoWindow`.
- [X] Move the "Save/Load Layout" functionality to a `File` menu in the `MainWindow`.

## Milestone 2: Cloud Integration - OneDrive & Google Drive
- [X] Research and integrate the Microsoft Authentication Library (MSAL) for OneDrive authentication.
- [X] Create an `OneDriveProvider` service that uses the Microsoft Graph API to fetch photos from the user's `/Pictures` folder.
- [X] Research and integrate Google's OAuth 2.0 client library for .NET.
- [X] Create a `GoogleDriveProvider` service that uses the Google Drive API to find and fetch photos.
- [X] Add icons and specific DataTemplates for each source type (Local, OneDrive, Google Drive) in the navigation pane.
- [X] Ensure the gallery correctly displays photos from these cloud sources, potentially downloading them on demand.

## Milestone 3: Advanced Source Integration
- [X] Research and implement iCloud Photos integration. This will likely involve finding the local directory used by Apple's "iCloud for Windows" application.
- [X] Create an `iCloudProvider` service. This service will find the local "iCloud Photos" folder and treat it as a local source.
- [X] Research methods for connecting to mobile devices (e.g., via MTP - Media Transfer Protocol).
- [X] Implement a `PhoneProvider` service to list and import photos from connected devices.
- [X] Add UI elements to initiate phone and iCloud sync.

## Milestone 4: Gallery Refinements
- [X] Implement sorting options for the gallery (by date, name, size).
- [X] Implement filtering or search functionality.
- [X] Add a "Favorites" or "Tagging" system.
- [X] Implement a "Recently Viewed" source that shows a history of opened photos.
- [X] Persist the list of configured sources between application sessions.
- [X] Refine UI/UX (added remove source functionality), add application icons, and improve performance with large photo collections (e.g., virtualization, caching).

## Milestone 5: Deployment
- [ ] Create an installer (e.g., using MSIX, WiX) that properly registers the application for file associations in the Windows Registry.

## Milestone 6: UI Polish and Refinements
- [X] Add "Gallery" source that aggregates photos from all other sources.
- [X] Default to selecting the Gallery tab on app startup.
- [X] Add settings cog to title bar with light/dark/system theme support.
- [X] Implement custom title bar with minimize, maximize, close buttons.
- [X] Add ascending/descending sort direction toggle.
- [X] Replace star favorites with heart icons.
- [X] Fix photo opening on single click instead of double click.
- [X] Fix photo window displaying gray screen instead of image.
- [X] Add navigation arrows to PhotoWindow for browsing photos.
- [X] Fix dark theme for all UI elements (scrollbars, menus, backgrounds, text).
- [X] Implement `JustifiedWrapPanel` with proper aspect ratio reading from image files.
- [X] Portrait/landscape photos now display at correct proportional widths.

---

### Completed Features
- Core multi-window viewer with zoom/pan.
- Save/Load of window layouts.
- Arrow key navigation and delete-to-recycle-bin in `PhotoWindow`.
- Command-line file opening.
- Gallery view aggregating photos from all sources.
- Settings persistence and theme system (Light/Dark/System).
- Custom title bar with window controls.
- Sort by direction (ascending/descending).
- Favorites system with heart icons.
- Photo navigation arrows in PhotoWindow.
- Justified gallery layout with true aspect ratio display (`JustifiedWrapPanel`).
- Pixel dimension reading from image files for accurate aspect ratios.