# Project Structure

```text
PhotoViewer/
├── PhotoViewer.sln
├── README.md
├── AGENTS.md
├── docs/
└── src/PhotoViewer/
    ├── Controls/       # Custom WPF panels and controls
    ├── Models/         # Persisted and domain data models
    ├── Services/       # Theme, settings, layout, and window services
    ├── *.xaml          # WPF views
    ├── *ViewModel.cs   # Presentation and application state
    ├── *Provider.cs    # Photo-source providers
    └── PhotoViewer.csproj
```

Production code belongs under `src/PhotoViewer/`. Automated tests, when added, belong under `tests/`. Application documentation belongs under `docs/`; repository-wide agent guidance remains at the root.

Generated `bin/` and `obj/` output, IDE metadata, OAuth secrets, and local credentials must not be committed.
