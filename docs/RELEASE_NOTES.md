# Release Notes

## 2026-03-03 — Phase 6 Naming & Identity Finalization

- Renamed solution file from temporary rename-window identity to `HomeCharts.sln`.
- Updated CI workflow `.github/workflows/migration-ci.yml` to restore/build/test the renamed solution.
- Updated runtime app data identity folder from temporary rename-window identity to `HomeCharts`.
- Completed naming cleanup pass for migration-facing workflow/docs/UI operational labels.
- Validation completed successfully:
  - `dotnet build HomeCharts.sln`
  - `dotnet test HomeCharts.sln`
  - `powershell -ExecutionPolicy Bypass -File build/publish-win-x64.ps1`
  - `dotnet run --project src/HomeCharts.App/HomeCharts.App.csproj -c Debug` (startup smoke test)
