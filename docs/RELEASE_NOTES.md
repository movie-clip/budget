# Release Notes

## 2026-09-25 — Potential New Rules: Single-List Redesign

### Changed

- Import tab "Potential New Rules" is now backed by exactly one list
  implementation end to end: `PreviewPotentialRulesUseCase` (extended) plus a
  new `ApplyPotentialRulesUseCase`/`PotentialRuleDraft` in
  `HomeCharts.Application`, and a new `PotentialRulesViewModel` +
  `PotentialRuleRowViewModel` in Presentation. The prior parallel
  "rules-queue" state on `MainWindowViewModel` (fields, commands, and the
  batch-save use case) is removed.
- `CreateCategorizationRulesBatchUseCase` is deleted. The single
  `CreateCategorizationRuleUseCase` (`Contains`, priority 5) remains the only
  rule-creation path; `ApplyPotentialRulesUseCase` wraps it for per-row Save
  and Apply All.
- Prefix-stripping (`DescriptionPrefixStripper`) and the assignable-category
  ordering/selection (`ExpenseCategoryCatalog`) moved from
  `MainWindowViewModel` into `HomeCharts.Domain`, so the suggested match
  string and default category are decided in one place and reused by both
  the list and `FormatDescriptionForDisplay`.
- **Unsaved edits now survive an in-list refresh.** Refresh Queue, a per-row
  Save, and Apply All all keep the description/category edits of every row
  still present in the recomputed list; only loading a different file, or a
  refresh triggered by importing/re-importing, resets rows to their
  suggested defaults. Previously every refresh dropped all unsaved edits.
- **Cosmetic:** a row with an unsaved edit is now highlighted (blue border),
  and the Apply All button shows the count of rows it would save, e.g.
  `Apply All (3)`.
- No schema, migration, or `HomeCharts.Contracts` change. Categorization
  precedence (`Exact > Regex > Contains`, priority tie-break) is unchanged.

### Validation

- `dotnet build HomeCharts.sln` ✅ (0 warnings, 0 errors)
- `dotnet build src/HomeCharts.App/HomeCharts.App.csproj` ✅ (0 warnings, 0 errors)
- `dotnet test HomeCharts.sln --no-build` ✅ (108 passing: Domain 17, Application 80, Infrastructure 11)

### Known Issues

- Not visually verified: the app was not launched for this change; only the
  AXAML compile was checked. A manual check of the modified-row highlight,
  the ComboBox default selection, and the bounded-region layout is still
  open.

## 2026-09-24 — Import Tab: Potential New Rules List Bounded Inside the Window

### Bug Fixes

- Import tab "Potential New Rules" list: the tab container now sits in the
  window's star-sized content row (`Grid.Row="3"` in `MainWindow.axaml`), so
  the list and the Expenses column get a finite height and scroll inside the
  window instead of growing past it.
- Potential New Rules pager header: the wrap panel no longer uses a fixed
  `ItemHeight`, and uses item/line spacing instead of per-item margins, so
  the header wraps onto extra lines without clipping.

### Known Issues

- Not visually verified: the app was not launched for this change. A manual
  check is still open at 1200x760 and 1400x900 with a large queue (list
  scrolls in-window, Expenses column scrolls, header wraps unclipped).
- Ledger tab container has the same missing `Grid.Row` and is not fixed by
  this change.

### Validation

- `dotnet build HomeCharts.sln` ✅
- `dotnet build src/HomeCharts.App/HomeCharts.App.csproj` ✅
- `dotnet test HomeCharts.sln --no-build` ✅ (56 passing)

## 2026-09-23 — Potential New Rules: Paging/Apply-All Fix

### Bug Fixes

- Import tab "Potential New Rules" list: category edits made on one page no
  longer disappear when navigating to another page via Prev/Next — edit state
  (`SelectedCategory`/`IsModified`) is now held on the full in-memory queue
  and reused across page renders instead of being rebuilt (and discarded)
  each time a page is displayed.
- "Apply All" now commits modified rows from every page of the potential
  rules queue, not just the page currently on screen; the button's
  enabled/disabled state also now reflects modified rows on any page, not
  only the displayed one.

### Validation

- `dotnet build HomeCharts.sln` ✅
- `dotnet build src/HomeCharts.App/HomeCharts.App.csproj` ✅
- `dotnet test HomeCharts.sln --no-build` ✅ (56 passing)

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
