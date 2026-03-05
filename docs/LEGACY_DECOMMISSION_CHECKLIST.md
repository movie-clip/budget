# Legacy Decommission Checklist

## Scope

Checklist to retire legacy WPF runtime dependency after migration cutover approval.

## Checklist

- [x] Migration runtime builds/tests/publish pipeline validated.
- [x] Migration naming and runtime identity finalization completed.
- [x] Backup/restore/export/import operational controls documented.
- [x] Cutover and rollback playbook documented.
- [x] Cutover rehearsal evidence recorded (`docs/CUTOVER_REHEARSAL_REPORT_2026-03-03.md`).
- [x] Dependency governance allowlist and audit cadence documented (`docs/DEPENDENCY_GOVERNANCE.md`).
- [x] Legacy runtime marked reference-only in migration plan and guidance.
- [x] Decommission approval prepared for execution at cutover gate.

## Decommission Gate Approval

- Engineering approval: ✅
- QA approval: ✅
- Release approval: ✅
- Approval date: 2026-03-03

## Execution-at-Cutover Actions

1. Disable legacy runtime distribution path.
2. Keep rollback artifact for one release cycle.
3. Archive legacy-only pipeline/components after stable migration release.
4. Record completion in release notes.
