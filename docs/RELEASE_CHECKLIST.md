# Release checklist

## Validation

- [ ] Run the manual matrix at target resolutions and DPI settings.
- [ ] Verify light/dark themes, keyboard navigation, screen-reader names, and reduced-motion behavior.
- [ ] Exercise local folders, inaccessible paths, permission errors, and folder changes.
- [ ] Exercise import duplicates, cancellation, failures, and large selections.
- [ ] Exercise viewer playback, editing, export formats, metadata, markup, and overwrite protection.
- [ ] Confirm editing never changes the source file and closing the viewer discards unsaved session edits.

## Packaging

- [ ] Select the Windows package/installer format.
- [ ] Register and uninstall file associations without changing defaults silently.
- [ ] Validate upgrades preserve settings, local source configuration, and workspace layout.
- [ ] Verify clean install, repair, uninstall, and rollback behavior.

## Diagnostics and handoff

- [ ] Confirm local diagnostics logging and log-file privacy behavior.
- [ ] Capture known limitations, Windows codec dependencies, and device/share integration gaps.
- [ ] Record version, commit, environment, and manual-matrix results.
