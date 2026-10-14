# Windows integration contract

PhotoViewer accepts one or more existing supported media paths as command-line arguments. Duplicate paths and existing files with unsupported extensions are ignored. Each remaining argument opens in the viewer; an invocation with no valid files opens the gallery.

Supported file extensions are centralized in `MediaFileTypes`:

- Images: `.jpg`, `.jpeg`, `.png`, `.bmp`, `.gif`
- Video: `.mp4`, `.mov`, `.avi`, `.mkv`, `.webm`

The eventual installer/package must register the application for these extensions and add an `open` verb that passes all selected files to the application. Registration should be per-user where possible, avoid changing an existing default handler without consent, and provide an uninstall rollback for every association it creates.

For development and portable deployments, `packaging/Register-PhotoViewerFileAssociations.ps1` provides a reversible per-user registration path:

```powershell
.\packaging\Register-PhotoViewerFileAssociations.ps1 -InstallPath "C:\Apps\PhotoViewer\PhotoViewer.exe"
.\packaging\Register-PhotoViewerFileAssociations.ps1 -Uninstall
```

The helper adds PhotoViewer to each extension's `OpenWithProgids` list without replacing the user's current default application. A signed installer or MSIX package should own the final production registration and invoke equivalent cleanup during uninstall.

Share-target and MTP integration remain separate capabilities; neither should be implied by file-association registration.
