# GUI Design Standards

These standards apply to new and modified WPF views in PhotoViewer.

## Theme compliance

- Use `{DynamicResource ...}` for colors and brushes that vary by theme.
- Do not introduce hard-coded foreground, background, border, or hover colors in view markup.
- Keep Light, Dark, and System behavior consistent across the main window, menus, dialogs, scrollbars, and photo windows.
- Preserve visible focus, hover, pressed, disabled, and selected states.

## Icons and actions

Use a clear icon or familiar visual symbol for primary actions, navigation, source types, favorites, deletion, settings, and window controls. Reuse the existing XAML icon resources where possible. Text may accompany an icon when it improves discoverability; avoid unexplained symbol-only controls.

## Gallery and sources

- Prefer thumbnails and representative source icons over file-path-only labels.
- Keep thumbnail cards consistent in spacing and interaction behavior.
- Preserve actual image proportions; use `JustifiedWrapPanel` for gallery layouts unless a feature requires another layout.
- Keep folder-tree indentation consistent and chevrons right-aligned.
- A chevron toggles expansion; selecting a row loads that folder's photos.
- Hide expansion affordances for leaf folders and show photo counts where available.

## Typography and layout

- Use the existing Segoe UI-based typography and the established spacing rhythm.
- Maintain clear hierarchy between title, source name, metadata, and secondary help text.
- Align related controls and provide comfortable click targets.
- Keep navigation visible without allowing controls to dominate the photo content.

## Interaction and accessibility

- Provide tooltips for icon-only controls and useful context for truncated paths or filenames.
- Support keyboard focus and the existing keyboard commands for photo navigation and deletion.
- Avoid cursor overrides unless the interaction genuinely changes the pointer mode.
- Use animation sparingly and never at the expense of gallery performance.

## Review checklist

- [ ] New colors use theme resources.
- [ ] Hover, pressed, focused, selected, and disabled states are visible.
- [ ] Actions have understandable icons, labels, or tooltips.
- [ ] Gallery thumbnails preserve aspect ratio.
- [ ] Layout and spacing match neighboring controls.
- [ ] Keyboard and accessibility behavior remains usable.
