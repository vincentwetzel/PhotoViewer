# Packaging templates

`PhotoViewer.Package.appxmanifest` is an MSIX association template. Before production use, the packaging pipeline must provide a real package identity, publisher certificate, versioning, logo assets, executable layout, signing, install/upgrade validation, and uninstall rollback.

The manifest registers the same media extensions accepted by `MediaFileTypes`. It does not implement share targets or phone/MTP transport.
