# PhotoViewer working backlog

Builds and test runs are intentionally skipped during this implementation pass.

PhotoViewer is intentionally local-only. Cloud providers, OAuth, iCloud synchronization, and cloud quota work are out of scope and have been removed.

Completed implementation details are retained in `docs/ARCHITECTURE.md`, `docs/SPEC.md`, and `docs/MANUAL_TEST_MATRIX.md`.

## Active backlog

### Device integration

- [ ] Implement phone/MTP discovery, permissions, and transport integration using the existing import workflow.

### Windows integration

- [ ] Complete production installer/MSIX packaging, signing, upgrade validation, and association registration.
- [ ] Add Windows share-target integration.
