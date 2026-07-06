# HomeCharts Operations

## 1. Build / Test / Run

### Build

`dotnet build HomeCharts.sln`

### Test

`dotnet test HomeCharts.sln --no-build`

### Run App

`dotnet run --project src/HomeCharts.App/HomeCharts.App.csproj -c Debug`

or use the helper script:

`build/run-app.ps1`

## 2. Packaging

Windows publish command:

`powershell -ExecutionPolicy Bypass -File build/publish-win-x64.ps1`

Publish output:

- `artifacts/publish/win-x64`

CI workflow `.github/workflows/migration-ci.yml` builds/tests/publishes the app artifact.

## 3. Pre-Release Checklist

1. Build and tests green.
2. Publish artifact generated successfully.
3. App launch smoke test on Windows host.
4. Import, merge, dashboard refresh, and rule workflows sanity-checked.
5. Backup/restore/export/import command flows smoke-tested.

## 4. Dependency Policy

Current runtime stack (high level):

- Avalonia UI packages
- Microsoft.Data.Sqlite

Test stack (high level):

- MSTest
- Microsoft.NET.Test.Sdk

Rules:

1. Any new external package requires PR rationale (purpose, alternatives, rollback impact).
2. Package updates must pass full validation (build/test/publish).
3. Do not introduce legacy-only dependencies into active `src/` projects.

## 5. Documentation Update Policy

When behavior changes, update in same PR:

- `PROJECT_GUIDE.md` for user workflow changes.
- `TECHNICAL_DESIGN.md` for architecture/contract changes.
- `RELEASE_NOTES.md` for shipped change summary.
