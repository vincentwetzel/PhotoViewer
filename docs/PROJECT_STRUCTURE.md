# Project Structure

```text
PhotoViewer/
├── PhotoViewer.sln
├── README.md
├── AGENTS.md
├── docs/
├── packaging/          # MSIX scaffolding and reversible file associations
├── tests/               # Automated service and model tests
└── src/PhotoViewer/
    ├── Controls/       # Custom WPF panels and controls
    ├── Models/         # Persisted and domain data models
    ├── Services/       # Theme, settings, layout, and window services
    ├── *.xaml          # WPF views
    ├── *ViewModel.cs   # Presentation and application state
    ├── *Provider.cs    # Photo-source providers
    └── PhotoViewer.csproj
```

Production code belongs under `src/PhotoViewer/`. Automated tests belong under `tests/`. Application documentation belongs under `docs/`; packaging assets belong under `packaging/`; repository-wide agent guidance remains at the root.

Generated `bin/` and `obj/` output, IDE metadata, and machine-specific local data must not be committed.
