# Dependency Governance (Migration Stack)

## Allowlist (Approved Runtime/Test Dependencies)

### Runtime

- `Avalonia` (`11.3.12`)
- `Avalonia.Controls.DataGrid` (`11.3.12`)
- `Avalonia.Desktop` (`11.3.12`)
- `Avalonia.Themes.Fluent` (`11.3.12`)
- `Avalonia.Fonts.Inter` (`11.3.12`)
- `Avalonia.Diagnostics` (`11.3.12`, debug-only assets in release)
- `Microsoft.Data.Sqlite` (`9.0.0`)

### Tests

- `Microsoft.NET.Test.Sdk` (`17.12.0` for migration test projects)
- `MSTest` (`3.6.4` for migration test projects)

### Legacy/Reference Scope (Not part of migration runtime)

- `tests/HomeCharts.Tests` and legacy WPF package graph remain reference-only until final repository decommission cleanup.

## Governance Rules

1. New external packages require a short ADR note in PR description with purpose, alternatives, and rollback impact.
2. Package version bumps must pass full baseline validation:
   - `dotnet build HomeCharts.sln`
   - `dotnet test HomeCharts.sln`
   - `powershell -ExecutionPolicy Bypass -File build/publish-win-x64.ps1`
3. Runtime dependency additions are limited to migration stack projects under `src/`.
4. Deprecated or legacy-only packages are not introduced into migration projects.

## Audit Cadence

- **Monthly**: dependency version review and vulnerability scan pass.
- **Per release**: re-check allowlist drift and update this document if changed.
- **Owner**: Migration engineering owner.

## Audit Record Template

- Date:
- Reviewer:
- Changed packages:
- Validation commands executed:
- Findings/risks:
- Follow-ups:
