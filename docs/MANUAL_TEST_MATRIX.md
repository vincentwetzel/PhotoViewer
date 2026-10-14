# PhotoViewer manual test matrix

This matrix is the repeatable smoke-test contract for the WPF interaction surface. Run it on a Windows 11 machine at 100% and 150% display scaling when a build is available.

| Area | Steps | Expected result |
| --- | --- | --- |
| Startup and theme | Launch; switch Light, Dark, and System in Settings | Shell and viewer resources update without restarting |
| Sources | Add a local folder; expand a subfolder; remove it from the context menu | Counts, selection, persistence, and safe removal work; files remain untouched |
| Gallery browsing | Search filename/date; clear search; switch media type; sort each option both ways | Debounced filtering, no-results state, totals, date headers, and active ordering are correct |
| Selection | Select tiles with mouse, Ctrl, Shift, Select all/none, and Escape | Selected count and action bar remain synchronized |
| Bulk actions | Favorite, copy, open with, move, and delete local selections | Local actions work and unsafe operations are skipped or explained |
| Import | Import multiple photos/videos; choose a destination; include duplicates; cancel midway | Progress, duplicate skipping, cancellation, and final summary are accurate |
| Drag and drop | Drop supported media and a folder onto the main window | Media opens; folder becomes a source; unsupported files are ignored |
| Viewer | Open image; use fit, actual size, menu zoom, wheel zoom, pan, filmstrip, Info, and keyboard shortcuts | Commands, persisted zoom/pan, metadata, and related navigation behave consistently |
| Video | Open supported and unsupported video files; play/pause, seek, volume, Space | Controls work where codecs are supported; fallback text is clear otherwise |
| Safety | Delete from gallery and viewer; cancel confirmation; remove a source | Recycle Bin semantics and source-removal semantics are preserved |
| Layout | Resize to minimum width; collapse navigation; maximize; close/reopen | Narrow rail, focus order, window bounds, and layout persistence remain usable |

Record failures with the scenario, Windows scaling, source type, and exact error text before changing behavior.
