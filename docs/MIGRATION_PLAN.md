# HomeCharts Migration Plan

One-page summary: `docs/MIGRATION_EXECUTIVE_SUMMARY.md`

## Status Legend

- ✅ Done
- 🟡 In Progress
- ⬜ Not Started

Document state: ✅ Closed (migration execution complete; retained for historical traceability)

## Current Snapshot

- Overall migration status: ✅ Complete
- Current active phase: ✅ Migration closure complete
- Backend foundation (Phases 0-2): ✅ Functionally complete for planned scope
- Latest validation: `dotnet build HomeCharts.sln`, `dotnet test HomeCharts.sln`, and publish script all green
- Most recent completed item: cutover rehearsal, dependency governance, and decommission approvals recorded
- Additional latest progress: Definition of Done closure checks finalized
- Phase 3 completion note: ledger filtering/paging + keyboard-first actions + SQLite-backed workflow integration coverage
- Phase 4 completion note: dashboard snapshot read-model + KPI/trend/category/uncategorized projections wired into UI

---

## 1) Scope & Intent — ✅ Done

This project is a migration (not a line-by-line port).

- Old WPF app is feature/spec reference only.
- New implementation prioritizes clean architecture and deterministic behavior.
- Technical debt is not carried over by default.

Target stack:

- UI: Avalonia
- Runtime: .NET (currently net9 for local compatibility)
- Data: SQLite + forward-only migrations
- Charts target: LiveCharts2 (Phase 4)

---

## 2) Architecture Principles — ✅ Done

Principles established and implemented in codebase boundaries:

1. UI has no business logic.
2. Domain/Application independent from Avalonia.
3. Feature-focused use-cases (not service sprawl).
4. Migration-driven schema evolution.
5. Deterministic categorization and import behavior.

---

## 3) Solution Structure — ✅ Done

Implemented structure:

- `src/HomeCharts.App`
- `src/HomeCharts.Presentation`
- `src/HomeCharts.Application`
- `src/HomeCharts.Domain`
- `src/HomeCharts.Infrastructure`
- `src/HomeCharts.Contracts`
- `tests/HomeCharts.Domain.Tests`
- `tests/HomeCharts.Application.Tests`
- `tests/HomeCharts.Infrastructure.Tests`

---

## 4) Delivery Phases

### Phase 0 — Baseline & Contracts — ✅ Done

Completed:

- Migration solution scaffolded (`HomeCharts.sln`)
- Layered projects + references wired
- Strict build settings in `Directory.Build.props`
- Baseline tests and build passing

Exit criteria: ✅ Met

---

### Phase 1 — Domain + Data Core — ✅ Done

Completed:

- Domain entities: transactions, categories, rules, import batches, manual overrides
- Persistence contracts added
- SQLite migration runner + schema `001_initial.sql`
- Repository implementations for core entities
- BankRecipes format institutionalized in `.github/copilot-instructions.md`
- Tests with copied realistic BankRecipes samples

Exit criteria: ✅ Met

---

### Phase 2 — Import + Categorization Engine — ✅ Done

Completed:

- Deterministic categorization engine (`Exact > Regex > Contains`)
- Tie-breaks: priority, then lexical stability
- Manual override-aware rule application
- Manual recategorization use case + override history persistence
- Data-quality improvement: `normalized_description` (`002_normalized_description.sql`)
- Import diagnostics (errors + warnings + metrics)
- In-file deterministic row dedup with skip reporting

Exit criteria: ✅ Met

---

### Phase 3 — New UI Shell + Ledger — ✅ Done

Completed:

- Minimal Avalonia shell wired through composition root
- MVP ledger/import/rule/manual recategorization flow wired to use-cases
- Presentation view model + command flow in place

Recent hardening completed:

- Added SQLite-backed end-to-end workflow integration coverage (import -> auto-categorize -> manual override -> re-apply rules)
- Added filterable/paged ledger query model with total-match reporting
- Added keyboard-first actions in shell (import, refresh, recategorize, search focus)
- Improved MVP import feedback with parsed/imported/duplicate/warning visibility

Exit criteria: ✅ Met

---

### Phase 4 — Dashboard + Insights — ✅ Done

Planned:

- KPI cards, trend charts, category distribution, uncategorized queue
- Snapshot query layer optimized for rendering

Delivered:

- `BuildDashboardSnapshotUseCase` added for single-pass snapshot projections:
	- KPI summary (income/expenses/net/savings/uncategorized)
	- Monthly trend points
	- Category expense breakdown
	- Uncategorized queue
- Dashboard snapshot wired into MVP UI with dedicated panels and summary line.
- Application tests added for dashboard snapshot correctness.

Exit criteria: ✅ Met

---

### Phase 5 — Hardening, Packaging, Cutover — ✅ Done

Planned:

- Backup/export/import operational commands
- Packaging pipeline
- Cutover + rollback playbook

Delivered:

- Added backup/restore use-cases and infrastructure maintenance service for SQLite database operations.
- Added export/import transfer-package use-cases for operational data movement.
- Wired maintenance operations into the MVP shell (backup/restore/export/import command flow).
- Added phase hardening tests for export/import and maintenance invocation scenarios.
- Added CI workflow for migration solution build/test/publish and publish script (`build/publish-win-x64.ps1`).
- Aligned CI naming to migration terminology (`.github/workflows/migration-ci.yml`, artifact `homecharts-migration-win-x64`).
- Added release packaging and cutover/rollback documentation.

Validation:

- `dotnet build HomeCharts.sln` ✅
- `dotnet test HomeCharts.sln` ✅
- `powershell -ExecutionPolicy Bypass -File build/publish-win-x64.ps1` ✅

Exit criteria: ✅ Met

---

### Phase 6 — Naming & Identity Finalization — ✅ Done

Purpose:

- Execute the delayed project/solution rename safely after migration stabilization.

Rename window checklist:

1. Freeze active feature merges for the rename branch window.
2. Rename solution and project files (`HomeCharts.sln` and related project identities).
3. Update CI/workflows/scripts/docs for new solution/project names.
4. Update assembly/product metadata and app identity strings.
5. Run full validation (`build`, `test`, publish script, app startup smoke-test).
6. Verify artifact naming and release notes references are consistent.
7. Merge with rollback plan ready (single-commit revert path documented).

Preparation completed:

- Rename-surface inventory captured across docs, workflows, scripts, and runtime identity touchpoints.
- Dedicated runbook and go/no-go template added: `docs/PHASE6_NAMING_FINALIZATION_RUNBOOK.md`.
- Solution renamed to `HomeCharts.sln` and CI pipeline references updated.
- Runtime app data identity moved from temporary rename-window identity to `HomeCharts`.

Delivered:

- Renamed solution file to `HomeCharts.sln`.
- Updated CI workflow references to new solution identity.
- Updated runtime identity folder from temporary rename-window identity to `HomeCharts`.
- Completed Phase 6 runbook go/no-go records and sign-off fields.
- Added release-note entry documenting rename finalization (`docs/RELEASE_NOTES.md`).

Exit criteria:

- No remaining legacy/temporary naming in solution/project/runtime identity.
- CI and packaging green under final names.

Exit criteria: ✅ Met

---

## 5) Cleanup Strategy (No-Garbage Migration) — ✅ Done

Completed:

- Migration implementation contained in `src/` and `tests/`
- Legacy planning artifacts removed from active migration branch
- Legacy WPF codebase and legacy CI workflow removed from repository root (migration-only structure retained).
- Naming cleanup pass completed for workflow labels, release/cutover docs, UI text, and solution/runtime identity.
- Dependency allowlist and audit cadence formalized (`docs/DEPENDENCY_GOVERNANCE.md`).
- Published artifact launch rehearsal logged with cutover evidence (`docs/CUTOVER_REHEARSAL_REPORT_2026-03-03.md`).
- Legacy decommission checklist approved for cutover execution (`docs/LEGACY_DECOMMISSION_CHECKLIST.md`).

Closure note:

- No open cleanup items remain in migration scope.

---

## 6) Definition of Done (Final Migration) — ✅ Done

Must be true at project completion:

- ✅ New stack is primary entry point
- ✅ No runtime dependency on legacy WPF app
- ✅ Domain/Application independently testable
- ✅ Import/rules/dashboard workflows validated on representative data
- ✅ Packaging + rollback documented and validated

---

## 7) Closure Queue (Completed)

1. ✅ Formalize dependency allowlist and recurring dependency-audit cadence
2. ✅ Run publish artifact rehearsal and log outcome
3. ✅ Execute cutover rehearsal from `CUTOVER_ROLLBACK_PLAYBOOK.md` with owner sign-offs
4. ✅ Finalize and approve legacy decommission checklist
5. ✅ Mark Definition of Done as complete after all closure checks pass

---

## 8) Validation Baseline

Current expected green commands:

- `dotnet build HomeCharts.sln`
- `dotnet test HomeCharts.sln`
- `powershell -ExecutionPolicy Bypass -File build/publish-win-x64.ps1`

---

## 9) Remaining Work to Finalize Migration

Final state:

- Migration finalization complete.
- No remaining migration-phase tasks.

Completion evidence:

- `docs/DEPENDENCY_GOVERNANCE.md`
- `docs/CUTOVER_REHEARSAL_REPORT_2026-03-03.md`
- `docs/LEGACY_DECOMMISSION_CHECKLIST.md`

