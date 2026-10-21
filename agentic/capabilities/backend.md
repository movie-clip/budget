# Capability pack: backend — project `budget`

For the `backend-engineer` lane. Everything about `src/HomeCharts.Domain/**`,
`src/HomeCharts.Application/**`, `src/HomeCharts.Infrastructure/**` and
`src/HomeCharts.Contracts/**` — the layers that decide, not the layers that
display. Where this contradicts general .NET instincts, this file wins.

Companion sources: `budget/CLAUDE.md`, `budget/AGENTS.md` (house style — this
pack does not repeat its naming/formatting/error-handling conventions in
full).

---

## Index

Read this block first. You are not expected to read this file end to end — read
what your order touches. Reading one extra section is cheap; acting on a
convention you never read is not.

**Always read:** **Guardrails, in code terms** · **Reuse — do not re-derive these** · **Gotchas from `AGENTS.md`** · **Definition of done for this lane**

| Section | Read it when |
|---|---|
| Layout | you are adding a file, or looking for where one lives |
| Migration conventions | your order adds or changes a SQL migration |
| Diagnosing a stuck bug | your first fix attempt didn't hold |

---

## Layout

```
HomeCharts.Domain/
  Model/            entities and value types
  Categorization/   deterministic rule-matching engine — Exact/Regex/Contains
HomeCharts.Application/
  UseCases/         *UseCase classes, one entry point per use case (ExecuteAsync)
  Import/           bank-statement parsing and import orchestration
HomeCharts.Infrastructure/
  Persistence/Sqlite/            Sqlite*Repository implementations
  Persistence/Sqlite/Migrations/ 001_initial.sql, 002_normalized_description.sql,
                                  003_transaction_fingerprint.sql, 004_prefix_filters.sql
HomeCharts.Contracts/
  Operations/       use-case-facing interfaces
  Persistence/       repository interfaces — CONTRACT SOURCE OF TRUTH for persisted data
```

Existing use cases, for finding prior art: `ImportBankStatementUseCase`,
`PreviewMatchedTransactionsUseCase`, `MergeMatchedTransactionsUseCase`,
`ApplyCategorizationRulesUseCase`, `BuildDashboardSnapshotUseCase`,
`InitializeDatabaseUseCase`.

## Migration conventions

- One numbered SQL file per schema change, additive only — never hand-edit
  `schema` in place of a new migration (guardrail 4).
- Keep migration versions ordered; `InitializeDatabaseUseCase` applies them at
  startup.
- A migration that changes a shape an existing `Sqlite*Repository` or view
  model already reads is a `design`-fires case (`phases.md` § "Why `design`
  fires whenever a migration is involved") — do not write the migration ahead
  of that pass deciding what happens to existing rows.

## Diagnosing a stuck bug

Your first fix attempt didn't hold: call the Skill tool with
`mattpocock-skills:diagnosing-bugs` rather than guessing again. Budget's own
`.claude/settings.json` enables this plugin.

Adapted to this lane's constraints:

- Diagnose the root cause and apply the fix — that part is yours.
- Do not write the regression test: name it in `handoff` for the test lane
  instead (you touch no test files, per the definition of done below).
- The skill's "ask the user" moments become a `risks` entry instead.
- Any scratch/repro file it has you create is named in `handoff` with its
  `git rm <path>` command — you have no delete tool.
- No commit. Your report is the protocol's report block, not a commit message.
- Record the call in `skill_calls:` (`PROTOCOL.md` § 7).

## Reuse — do not re-derive these

- `HomeCharts.Domain/Categorization/**` — the only rule-matching engine.
  `Exact > Regex > Contains` precedence and priority-then-lexical tie-breaking
  live here; do not reimplement matching in a use case or repository.
- `SqliteMapping` helpers — date, bit and timestamp conversions for SQLite.
  Reuse them rather than hand-rolling a conversion in a new repository.
- Existing `Sqlite*Repository` implementations, for the parameterized-command
  pattern — never interpolate values into SQL text.

## Guardrails, in code terms

**Deterministic categorization (guardrail 1).** A change to
`Categorization/**` that alters precedence, tie-breaking, or introduces
nondeterminism (wall-clock time, unordered iteration, floating-point
comparison without an explicit tolerance policy) is a guardrail violation, not
a style note.

**Manual overrides (guardrail 2).** `ApplyCategorizationRulesUseCase` and any
reapply flow must check override history before assigning a category and must
never overwrite a manual choice. A "fix" that drops override history to
simplify the flow is a regression.

**Layering (guardrail 3).** Business logic — matching, import decisions,
dashboard aggregation — belongs in `Domain`/`Application`, never leaks into a
repository beyond persistence concerns, and is never added to `Presentation`
or `App` from this lane's side of the contract.

**Schema (guardrail 4).** No raw hand-edit of the SQLite schema without a
numbered migration. `Contracts/Persistence` interfaces change in the same
slice as the migration that backs them, never in a later, separate one.

**Build correctness (guardrail 5).** Source projects treat warnings as
errors. Suppressing a warning (`#pragma`, `NoWarn`, severity override) to get
a green build is a defect to report in `risks`, not a fix to apply silently.

## Gotchas from `AGENTS.md`

- **No separate lint script.** `dotnet build` is the lint/analyzer gate for
  source projects; there is nothing else to run.
- **Test projects do not use warnings-as-errors** the way source projects do
  — do not expect an analyzer warning inside `tests/` to fail a build the way
  one inside `src/` would.
- **`vitest`/Node-style test-runner drift does not apply here** — MSTest is
  configured for method-level parallelization via `MSTestSettings.cs`; a
  flaky-looking failure may be a parallelization/shared-state issue rather
  than a real regression. Check for shared mutable state before assuming a
  test is wrong.
- **CI (`migration-ci.yml`) builds and tests in `Release` configuration.**
  Local iteration commonly defaults to `Debug`; a change verified only in
  Debug has not been verified against what CI actually gates on.

## Definition of done for this lane

- [ ] Categorization precedence and tie-breaking unchanged, or the change is
      the explicit point of the order
- [ ] Manual override history preserved through any reapply path touched
- [ ] No business logic added to a repository beyond persistence concerns
- [ ] Schema change accompanied by a new numbered migration, not a hand edit
- [ ] `Contracts` interfaces updated in the same slice as the migration/use
      case change they back
- [ ] Reused an existing SQLite mapping/repository pattern instead of
      re-deriving one
- [ ] `contract_notes` names any `Contracts` interface or use-case output
      shape change that now needs a matching Presentation/App update
- [ ] No test files touched — that is the test lane
