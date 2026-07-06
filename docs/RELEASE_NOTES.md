# Release Notes

## 2026-03-05 — Documentation Reset + Dashboard/Category UX Updates

### Documentation

- Replaced migration-era doc set with a current-state documentation structure:
  - `docs/README.md`
  - `docs/PROJECT_GUIDE.md`
  - `docs/TECHNICAL_DESIGN.md`
  - `docs/OPERATIONS.md`
- Removed obsolete migration cutover/runbook/checklist docs.

### Product/UX

- Dashboard monthly trend uses dynamic income/expense bars with legend.
- Dashboard expenses panel now:
  - shows all non-zero categories,
  - supports accordion expand/collapse,
  - shows per-category transaction dropdown list with constrained height,
  - uses hover highlight styling with transparent base rows,
  - stretches list elements to full list width.

### Categorization

- Added canonical categories `Income` and `Transfer`.
- `Income` is auto-assigned in rule application for positive transactions without a rule match.

### Validation

- `dotnet build HomeCharts.sln` ✅
- `dotnet test HomeCharts.sln --no-build` ✅ (41 passing)

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
