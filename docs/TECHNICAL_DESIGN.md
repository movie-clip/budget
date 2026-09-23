# HomeCharts Technical Design

## 1. Architecture

HomeCharts uses a layered architecture with strict boundaries:

- `HomeCharts.App` — Avalonia shell/composition root
- `HomeCharts.Presentation` — view models, UI state, commands
- `HomeCharts.Application` — use-cases and orchestration
- `HomeCharts.Domain` — entities and deterministic categorization engine
- `HomeCharts.Infrastructure` — SQLite persistence + migrations
- `HomeCharts.Contracts` — repository and operation interfaces

Core rule: no business logic in Avalonia code-behind.

## 2. Runtime Composition

`src/HomeCharts.App/CompositionRoot.cs` wires concrete repositories/use-cases into `MainWindowViewModel`.

SQLite DB path is under local app data (`HomeCharts` folder), with migrations applied at startup via `InitializeDatabaseUseCase`.

## 3. Key Use-Cases

- `ImportBankStatementUseCase`
  - Parses file
  - Detects duplicate import hash
  - Deduplicates rows in-file and against DB fingerprints
  - Persists transactions + import batch metadata

- `PreviewMatchedTransactionsUseCase`
  - Parses file and applies rules in preview-only mode

- `MergeMatchedTransactionsUseCase`
  - Persists only matched rows
  - Stores import batch and duplicate skip metrics

- `ApplyCategorizationRulesUseCase`
  - Applies deterministic rule matching to date range
  - Respects manual overrides
  - Auto-assigns `Income` category for positive transactions when no rule matches

- `BuildDashboardSnapshotUseCase`
  - Produces read-model for KPI, monthly trend, category breakdown, uncategorized queue, month-over-month comparison, coverage, largest expenses, and recurring expenses
  - `MonthComparison` (current-vs-previous month deltas) and the uncategorized queue are consumed by the Dashboard UI (the month-comparison strip and the Needs Attention panel, respectively)

## 4. Deterministic Categorization

Matching policy:

1. `Exact`
2. `Regex`
3. `Contains`

Tie-breaking:

1. higher priority first
2. stable lexical order

Manual override history is preserved and prevents overwrite in auto-reapply passes.

## 5. Data Model Highlights

Important persisted concepts:

- `transactions`
  - raw + normalized description
  - fingerprint for deduplication
  - optional source account and external reference
  - category assignment

- `categorization_rules`
  - match type, pattern, priority, active flag

- `import_batches`
  - source name, file hash, imported/skipped counts

- `manual_overrides`
  - transaction/category override history

- prefix-filter table
  - display/matching prefix cleanup configuration

All schema changes must go through migration files.

## 6. UI Design Notes

Dashboard expenses panel supports:

- full non-zero category listing,
- single-open accordion behavior,
- hover-highlighted rows,
- constrained dropdown list for category transactions.

Trend panel shows side-by-side monthly income/expense bars.

## 7. Testing Strategy

- `HomeCharts.Application.Tests`
  - use-case behavior and deterministic logic
- `HomeCharts.Infrastructure.Tests`
  - SQLite-backed workflow integration
- `HomeCharts.Domain.Tests`
  - domain-level correctness

Baseline validation command set:

- `dotnet build HomeCharts.sln`
- `dotnet test HomeCharts.sln --no-build`

## 8. Extension Guidance

For new features:

1. Add/extend use-cases in `Application` first.
2. Keep persistence contracts in `Contracts`.
3. Implement repositories/migrations in `Infrastructure`.
4. Bind from `Presentation` view models.
5. Keep `App` layer for wiring and UI shell only.

When adding categories, keep canonical ordering synchronized between:

- seeded defaults,
- presentation ordering,
- tests expecting category lists.
