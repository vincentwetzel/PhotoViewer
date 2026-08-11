# Project To-Do List

## Repository maintenance

- [X] Standardize application source under `src/PhotoViewer/`.
- [X] Centralize project documentation under `docs/`.
- [X] Add AI indexing exclusions and refresh `.gitignore` coverage.
- [X] Refresh documentation to match the current source tree and implemented features.

## Implemented gallery and source work

- [X] Gallery shell with source navigation and thumbnail grid.
- [X] Local folder provider and expandable folder tree.
- [X] OneDrive and Google Drive providers with authentication services.
- [X] Local iCloud Photos provider.
- [X] Gallery, Favorites, and Recently Viewed collections.
- [X] Sorting, filtering, single-click opening, favorite persistence, and source persistence.
- [X] Photo caching, progressive batch updates, five-minute staleness detection, and folder watchers.
- [X] Justified layout using image pixel dimensions for aspect ratios.
- [X] Theme selection, custom title bar, window controls, and dynamic theme resources.
- [X] Photo-window zoom, pan, keyboard/overlay navigation, deletion to Recycle Bin, and layout save/load.
- [X] Direct file launch through a command-line path.

## Known gaps

- [ ] Implement phone/MTP photo import.
- [ ] Implement an actual iCloud sync workflow; the current provider reads the local iCloud Photos folder.
- [ ] Add automated tests under `tests/`.
- [ ] Add packaging/installer support and formal Windows file-association registration.
