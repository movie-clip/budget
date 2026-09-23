# Release Notes

## 2026-09-23 — Cash Flow Trend Chart: Data-Accuracy Fixes + Redesign

### Data Accuracy

- Dashboard/ledger date-range transaction reads (`GetByDateRangeAsync`) now
  filter out soft-deleted rows (`is_deleted = 0`), matching the existing
  fingerprint-lookup query in the same repository.
- Monthly trend window anchor is now capped at today
  (`Min(DateOnly.Today, Max(BookingDate))`), so a future-booked transaction
  can no longer pull the 12-month trend window forward past the current
  month.

### Product/UX

- Cash Flow Trend chart: income and expense bars are now scaled against one
  shared maximum across both series, so bar heights are comparable in
  absolute terms (previously each series was scaled independently against
  its own peak).
- Cash Flow Trend chart redesigned: gradient-filled income/expense bars,
  per-month hover highlight, a signed net indicator (green/red) per month,
  and a restyled header with a subtitle line, range pill, and net legend
  swatch.

### Validation

- `dotnet build HomeCharts.sln` ✅
- `dotnet build src/HomeCharts.App/HomeCharts.App.csproj` ✅
- `dotnet test HomeCharts.sln --no-build` ✅ (53 passing)

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
