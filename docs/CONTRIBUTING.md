# Contributing

Before substantial changes, open an issue or discuss the intended behavior with the project maintainers. Keep changes focused and avoid committing generated output or credentials.

## Before submitting a change

- Build with `dotnet build PhotoViewer.sln`.
- Update the relevant documentation and `docs/CHANGELOG.md` for user-visible behavior.
- Mark completed work in `docs/TODO.md` when the change corresponds to an item there.
- For UI changes, follow [GUI Design Standards](GUI_DESIGN_STANDARDS.md), including theme resources, icons, keyboard/focus feedback, and accessible sizing.
- Add or update tests under `tests/` when test infrastructure exists.

## Pull requests

Describe the user-visible result, affected sources or integrations, verification performed, and any configuration required by reviewers. Do not include OAuth secrets, token files, or machine-specific paths.
