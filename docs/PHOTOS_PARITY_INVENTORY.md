# Windows 11 Photos parity inventory

This inventory compares the current shell and gallery behavior with the
reference screenshots in `reference-images/windows-11-photos/`.

## Implemented foundations

- Shared theme resources and interaction states for surfaces, typography,
  spacing, borders, focus, selected rows, and compact actions.
- Photos-style shell with app identity, search, Import and Settings actions,
  navigation, and custom window controls.
- Gallery metadata, visible counts, sorting, filtering, date grouping, and
  explicit empty/no-results states.
- Selection model with tile checkboxes and bulk local actions.
- Collapsible navigation rail, thumbnail density, filmstrip navigation, image
  editing workspace, local import flow, and video playback.

## Remaining work

- Account status, richer media/duplicate indicators, visual comparison captures,
  and accessibility/reduced-motion review.
- Device/MTP import and Windows share-target integration.
- Broader automated interaction coverage and production packaging validation.

## Intentionally deferred

- Exact Windows Photos branding and proprietary icons.
- Cloud providers and synchronization; PhotoViewer is local-only.
- Video editing. Playback depends on Windows codec availability.
