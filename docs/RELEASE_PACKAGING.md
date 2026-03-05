# Release Packaging

## Windows Package (Current)

Use the publish script:

`powershell -ExecutionPolicy Bypass -File build/publish-win-x64.ps1`

Output:

- `artifacts/publish/win-x64`

## CI Packaging

GitHub Actions workflow `.github/workflows/migration-ci.yml` runs:

1. restore/build/test on migration solution
2. publish `HomeCharts.App`
3. upload publish artifact

Workflow scope note:

- `migration-ci.yml` is path-scoped to migration files (`src/`, migration tests, build/docs, migration solution/workflow).
- Legacy workflow `.github/workflows/ci-tests.yml` remains path-scoped to legacy WPF/test files.

## Pre-release Checklist

- `dotnet test HomeCharts.sln` is green
- publish artifact launches on validation Windows host (repeat on clean Windows VM when available)
- backup/restore/export/import commands smoke-tested
- migration runner applies all scripts on empty DB
