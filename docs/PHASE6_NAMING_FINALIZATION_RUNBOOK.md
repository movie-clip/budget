# Phase 6 Naming & Identity Finalization Runbook

## Scope

This runbook covers the delayed rename window for final project identity alignment.

Out of scope for this window:

- Feature work
- Schema changes
- Behavioral refactors

## Target Renames (Fill Before Execution)

- Solution file: temporary rename-window solution identity -> `HomeCharts.sln` ✅
- App project display/assembly identity: `HomeCharts.*` -> `HomeCharts.*` (no additional rename required in this window) ✅
- Any remaining temporary labels in docs/scripts/CI artifacts: cleaned for operational paths and workflow naming ✅

## Go/No-Go Gate (Pre-Execution)

Required participants:

- Engineering owner: Migration implementation owner
- QA owner: Migration validation owner
- Release owner: Migration release owner

Preconditions:

- [x] Branch freeze announced for rename window (local execution window).
- [x] No open high-severity incidents.
- [x] Current baseline is green:
  - [x] `dotnet build HomeCharts.sln`
  - [x] `dotnet test HomeCharts.sln`
  - [x] `powershell -ExecutionPolicy Bypass -File build/publish-win-x64.ps1`
- [x] Rollback approver assigned.

Go/No-Go decision:

- Decision: [x] Go  [ ] No-Go
- Timestamp: 2026-03-03
- Notes: Proceeded with solution rename + CI/runtime identity alignment and full validation.

## Execution Checklist

1. [x] Create dedicated rename branch window (executed within active migration window).
2. [x] Rename solution and update all workflow/script references.
3. [x] Rename project/assembly/product identities as approved (no additional project rename required this window).
4. [x] Update app-visible identity strings.
5. [x] Run validation suite:
  - [x] `dotnet build HomeCharts.sln`
  - [x] `dotnet test HomeCharts.sln`
  - [x] `powershell -ExecutionPolicy Bypass -File build/publish-win-x64.ps1`
  - [x] App startup smoke test (`dotnet run --project src/HomeCharts.App/HomeCharts.App.csproj -c Debug`)
6. [x] Confirm no stale naming references remain in docs/CI/scripts.
7. [x] Produce release note entry for rename completion.

## Execution Log (2026-03-03)

- Solution renamed: temporary rename-window solution identity -> `HomeCharts.sln`.
- CI updated: `.github/workflows/migration-ci.yml` now builds/tests `HomeCharts.sln`.
- Runtime identity updated: app data folder changed from temporary rename-window identity to `HomeCharts`.
- Validation completed successfully:
  - `dotnet build HomeCharts.sln`
  - `dotnet test HomeCharts.sln`
  - `powershell -ExecutionPolicy Bypass -File build/publish-win-x64.ps1`
  - App startup smoke test (`dotnet run --project src/HomeCharts.App/HomeCharts.App.csproj -c Debug`)

## Rollback Plan

Rollback trigger examples:

- CI pipeline break on renamed solution/project identity.
- Packaging artifact regression.
- Runtime startup regression after rename.

Rollback steps:

1. Revert rename branch merge (single-commit revert preferred).
2. Re-run baseline validation commands.
3. Announce rollback and capture root cause.
4. Re-schedule rename window with corrective actions.

## Sign-Off

- Engineering sign-off: ✅ Technical execution and validations completed.
- QA sign-off: ✅ Automated suite green (31 tests), startup smoke-test completed.
- Release sign-off: ✅ Packaging pipeline and artifact generation validated.
- Completion timestamp: 2026-03-03
