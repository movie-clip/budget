# HomeCharts

HomeCharts is a desktop budgeting application built with Avalonia, .NET, and SQLite.

## Quick Start

### Build

`dotnet build HomeCharts.sln`

### Test

`dotnet test HomeCharts.sln --no-build`

### Run

`dotnet run --project src/HomeCharts.App/HomeCharts.App.csproj -c Debug`

or

`build/run-app.ps1`

## Documentation

- [Documentation Index](docs/README.md)
- [Project Guide (features and workflows)](docs/PROJECT_GUIDE.md)
- [Technical Design](docs/TECHNICAL_DESIGN.md)
- [Operations](docs/OPERATIONS.md)
- [Release Notes](docs/RELEASE_NOTES.md)

## Repository Layout

- `src/` — application projects
- `tests/` — automated tests
- `build/` — helper scripts
- `docs/` — project documentation

## Notes

- Legacy WPF code is reference-only for behavior intent.
- Active implementation and ongoing development are in the `src/HomeCharts.*` projects.
