# AGENTS.md

This file gives repository-specific guidance to coding agents working in `C:\projects\NET\budget`.

## Scope

- Applies to the whole repository.
- Prefer existing patterns over generic .NET defaults.
- Treat old WPF code as reference-only; active work is in `src/HomeCharts.*`.

## Product And Architecture

- HomeCharts is a desktop budgeting app built with Avalonia, .NET 9, and SQLite.
- Architecture is layered and intentionally strict:
- `HomeCharts.App` = Avalonia shell and composition root only.
- `HomeCharts.Presentation` = view models, UI state, commands, display models.
- `HomeCharts.Application` = use cases and orchestration.
- `HomeCharts.Domain` = business rules and deterministic categorization logic.
- `HomeCharts.Infrastructure` = SQLite persistence and migrations.
- `HomeCharts.Contracts` = repository and operation interfaces.
- Keep business logic out of Avalonia code-behind.
- Keep schema changes in SQL migration files under `src/HomeCharts.Infrastructure/Persistence/Sqlite/Migrations`.

## Repository Rules From Copilot Instructions

- This project is a migration to Avalonia + .NET + SQLite.
- Prefer clean architecture boundaries over legacy compatibility.
- Bank files in `BankRecipes/*.txt` are pipe-delimited with 7 columns.
- Accept empty optional fields such as external reference and source account.
- Preserve original descriptions while also supporting normalized matching fields.
- Categorization must be deterministic.
- Rule precedence target is `Exact > Regex > Contains`.
- Tie-breaking target is higher priority first, then stable lexical rule order.
- Support manual overrides and preserve override history.
- Tests should use realistic rows copied from `BankRecipes` samples.
- No Cursor rules were found in `.cursor/rules/` or `.cursorrules`.

## Toolchain

- SDK target is `.NET 9`.
- Test framework is MSTest.
- CI runs on Windows.
- Source projects enable nullable reference types, implicit usings, latest analyzers, latest language version, and warnings-as-errors.
- Test projects enable nullable reference types, implicit usings, and latest language version.

## Build Commands

- Restore: `dotnet restore HomeCharts.sln`
- Build Avalonia app project directly: `dotnet build src/HomeCharts.App/HomeCharts.App.csproj`
- Build all projects: `dotnet build HomeCharts.sln`
- Build Release without restore: `dotnet build HomeCharts.sln --configuration Release --no-restore`
- Build and run helper script: `pwsh ./build/build-and-run-app.ps1`
- Publish app: `pwsh ./build/publish-win-x64.ps1`
- Direct publish equivalent:
- `dotnet publish src/HomeCharts.App/HomeCharts.App.csproj -c Release -r win-x64 --self-contained false -o artifacts/publish/win-x64`

## Run Commands

- Run app in Debug: `dotnet run --project src/HomeCharts.App/HomeCharts.App.csproj -c Debug`
- Run helper script: `pwsh ./build/run-app.ps1`
- Local app database is created under local app data by `CompositionRoot`.

## Test Commands

- Run all tests: `dotnet test HomeCharts.sln --no-build`
- Run all tests in Release CI style: `dotnet test HomeCharts.sln --configuration Release --no-build`
- Run one test project:
- `dotnet test tests/HomeCharts.Application.Tests/HomeCharts.Application.Tests.csproj`
- `dotnet test tests/HomeCharts.Domain.Tests/HomeCharts.Domain.Tests.csproj`
- `dotnet test tests/HomeCharts.Infrastructure.Tests/HomeCharts.Infrastructure.Tests.csproj`
- Run a single MSTest by fully qualified name:
- `dotnet test tests/HomeCharts.Application.Tests/HomeCharts.Application.Tests.csproj --filter "FullyQualifiedName~HomeCharts.Application.Tests.ApplicationUseCaseTests.InitializeDatabaseUseCase_CallsMigrationRunner"`
- Run a single test by method name:
- `dotnet test tests/HomeCharts.Infrastructure.Tests/HomeCharts.Infrastructure.Tests.csproj --filter "Name~SqliteBackedWorkflow_MergeMatchedTransactions_FromTwoDifferentFiles_AppendsToBudget"`
- Run a whole test class:
- `dotnet test tests/HomeCharts.Domain.Tests/HomeCharts.Domain.Tests.csproj --filter "ClassName=HomeCharts.Domain.Tests.DomainModelTests"`
- Additional current class filters:
- `dotnet test tests/HomeCharts.Application.Tests/HomeCharts.Application.Tests.csproj --filter "ClassName=HomeCharts.Application.Tests.Phase5OperationsTests"`
- `dotnet test tests/HomeCharts.Infrastructure.Tests/HomeCharts.Infrastructure.Tests.csproj --filter "ClassName=HomeCharts.Infrastructure.Tests.InfrastructurePersistenceTests"`
- `dotnet test tests/HomeCharts.Infrastructure.Tests/HomeCharts.Infrastructure.Tests.csproj --filter "ClassName=HomeCharts.Infrastructure.Tests.WorkflowIntegrationTests"`
- When iterating on one test, prefer targeting the specific test project instead of the solution.
- MSTest is configured for method-level parallelization via `MSTestSettings.cs`.

## Lint And Validation

- No separate lint script or formatter script was found.
- Use `dotnet build` as the main lint/analyzer gate for source projects.
- Source builds treat warnings as errors, so analyzer warnings must be fixed, not ignored.
- For any Avalonia/XAML/UI change, explicitly run `dotnet build src/HomeCharts.App/HomeCharts.App.csproj` before finishing; solution tests alone are not sufficient to catch malformed XAML.
- Before finishing substantial changes, run at least `dotnet build HomeCharts.sln` and the most relevant `dotnet test ...` command.
- CI validates restore, Release build, Release test, and publish.

## Code Organization

- Put interfaces in `HomeCharts.Contracts`.
- Put domain models and deterministic matching logic in `HomeCharts.Domain`.
- Put orchestration and use-case entry points in `HomeCharts.Application`.
- Put SQLite implementations in `HomeCharts.Infrastructure`.
- Put UI state and command wiring in `HomeCharts.Presentation`.
- Keep `HomeCharts.App` focused on startup, Avalonia wiring, and composition root concerns.

## Imports And Namespaces

- Use file-scoped namespaces.
- Order `using` directives with framework namespaces first and `HomeCharts.*` namespaces after.
- Keep a blank line between framework and project `using` groups when both are present.
- Do not add handwritten global usings; rely on implicit usings unless a file genuinely needs an explicit import.
- Prefer simple, direct imports over aliasing.

## Formatting

- Use 4-space indentation.
- Use Allman braces.
- Keep lines readable; do not compress complex expressions to one line.
- Expression-bodied members are fine for very small members only.
- Use raw string literals for multiline SQL.
- Use collection expressions like `[]` where they fit the existing style.
- Preserve the existing spacing style around object initializers, switch expressions, and lambda blocks.

## Type Usage

- Nullable reference types are enabled; annotate reference nullability correctly.
- Prefer `record` for immutable data carriers, result objects, and view-model row types.
- Prefer `sealed` for concrete classes and records unless inheritance is intentional.
- Use primary constructors for dependency-heavy classes when it matches existing code.
- Store injected dependencies in private readonly fields with `_camelCase` names.
- Use `var` when the right-hand side makes the type obvious; use explicit types when clarity is better.
- Prefer `IReadOnlyList<T>`, `IReadOnlyCollection<T>`, and `IReadOnlySet<T>` in contracts where mutation is not intended.

## Naming Conventions

- Use PascalCase for types, methods, properties, enums, and public members.
- Use camelCase for locals and parameters.
- Use `_camelCase` for private fields.
- Suffix asynchronous methods with `Async`.
- Use `*UseCase` for application entry-point classes.
- Use `*Result`, `*Item`, `*Row`, and `*ViewModel` for DTO-style records following current patterns.
- Repository implementations should be named `Sqlite*Repository` in infrastructure.
- Test names follow `Method_Scenario_Outcome` and should stay descriptive.

## Error Handling And Control Flow

- Handle expected validation failures through result objects when that is the established pattern.
- Throw exceptions for truly exceptional infrastructure problems, such as missing files or invalid package schema.
- Catch narrow exceptions where possible; avoid broad catch blocks in domain and application layers.
- A broad catch exists at the UI busy-runner boundary to surface status messages; do not copy that pattern into lower layers unless there is a strong reason.
- Return early for guard clauses and invalid input.
- Preserve deterministic ordering whenever multiple matches are possible.

## Domain And Data Rules

- Preserve deterministic categorization behavior.
- Keep precedence `Exact > Regex > Contains`.
- Preserve tie-breaking by priority and stable lexical order.
- Preserve manual override history and do not overwrite manual choices during auto-apply flows.
- Normalize descriptions for matching, but keep original descriptions for storage and display scenarios that need them.
- Accept noisy uppercase bank descriptions and optional empty columns from bank imports.
- Keep realistic sample rows in tests aligned with the bank recipe format.

## Persistence And SQL

- All schema changes must be implemented as numbered SQL migrations.
- Keep migration versions ordered and additive.
- Prefer parameterized commands; existing SQLite repositories consistently bind parameters instead of interpolating values.
- Reuse `SqliteMapping` helpers for date, bit, and timestamp conversions.
- Keep repository methods async and cancellation-aware.
- When reading from the database, convert nulls explicitly and preserve domain types such as `DateOnly`, `Guid`, and `DateTimeOffset`.

## Application Layer Guidance

- Keep use cases small, explicit, and task-oriented.
- Use `ExecuteAsync(...)` as the standard entry point name.
- Accept `CancellationToken cancellationToken = default` on async public methods in application and infrastructure layers.
- Return dedicated result records rather than tuples for externally consumed use cases.
- Keep orchestration in application services; do not push UI concerns or persistence details upward.

## Presentation Layer Guidance

- Keep `MainWindowViewModel` and related view models responsible for UI state, command enablement, and presentation formatting.
- Do not move business rules into Avalonia views or code-behind.
- Use `ObservableObject` and `SetProperty` for state changes.
- Prefer explicit status text updates for user-visible operations.
- Keep formatting helpers in presentation when they are purely display-focused.

## Testing Guidance

- Add tests in the layer that owns the behavior.
- Use `HomeCharts.Domain.Tests` for pure domain logic.
- Use `HomeCharts.Application.Tests` for use-case behavior and orchestration.
- Use `HomeCharts.Infrastructure.Tests` for SQLite-backed workflows and migration-sensitive behavior.
- Prefer realistic bank statement rows over synthetic placeholders.
- Cover deterministic ordering, duplicate detection, migration behavior, manual override preservation, and parse edge cases.
- When adding migrations or persistence behavior, include at least one integration-style test if practical.

## Agent Workflow Tips

- Start by checking whether the change belongs in Domain, Application, Infrastructure, Presentation, or App.
- Mirror existing naming and result-record patterns before introducing new abstractions.
- Do not invent new repository layers or service containers unless the repo already needs them.
- If you change persistence shape, add or update a migration and relevant tests together.
- If you change categorization behavior, verify deterministic precedence and tie-breaking explicitly.
- Prefer the smallest change that fits the current architecture.
