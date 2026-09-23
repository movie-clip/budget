# CLAUDE.md

This repo is bound into the `.agentic` orchestration network (`budget/.agentic.json`
points at `../.agentic`, project `budget`). If you were dispatched as a lane, your
order and `<agenticRoot>/projects/budget/project.md` govern; this file is the
fast, always-loaded context every session gets, agentic or not.

## Product

HomeCharts is a desktop budgeting app (Avalonia UI, .NET 9, SQLite, local-only —
no server, no sync, no accounts). It:

- imports bank statement files (`BankRecipes/*.txt`, 7-column pipe-delimited),
- categorizes transactions deterministically against user-defined rules,
- supports manual category overrides that survive re-categorization,
- merges reviewed transactions into the local SQLite budget database,
- shows a dashboard: KPI cards (income/expenses/uncategorized), a monthly
  trend chart, and an expenses-by-category panel.

Legacy WPF code in the repo is reference-only for behavior intent — active
implementation is entirely under `src/HomeCharts.*`.

## Hard guardrails

- **Deterministic categorization.** Rule precedence is `Exact > Regex > Contains`;
  ties break by higher priority, then stable lexical rule order. Never make a
  categorization outcome depend on anything nondeterministic.
- **Manual overrides are never silently overwritten.** Auto-apply and
  rule-reapply flows preserve override history and must not replace a user's
  manual category choice.
- **No business logic in `HomeCharts.App` or `HomeCharts.Presentation`.**
  Decisions belong in `HomeCharts.Domain` / `HomeCharts.Application`; the
  Avalonia shell and view models format, display and dispatch — they do not
  decide.
- **Schema changes only through numbered SQL migrations** under
  `src/HomeCharts.Infrastructure/Persistence/Sqlite/Migrations`. No hand-edited
  schema without an accompanying migration file.
- **Warnings-as-errors is the correctness gate.** Source projects build with
  analyzers and warnings as errors; silencing a warning to get a build green
  is a defect to fix, not a build setting to relax.

Absolute, and unnumbered on purpose: the agentic network's profile
(`.agentic/projects/budget/project.md` § Hard guardrails) carries the same
five in the numbering the protocol cites, so a `REFUSED: guardrail 3` from a
dispatched lane names exactly one thing.

`AGENTS.md` restates these in more detail and is the deeper source on house
style (naming, formatting, error handling, CI caveats) — not superseded by
this file, just not repeated here.

## Canonical docs

| Doc | Covers |
|---|---|
| `docs/PROJECT_GUIDE.md` | user-facing flows, the category model |
| `docs/TECHNICAL_DESIGN.md` | architecture, use cases, data model |
| `docs/OPERATIONS.md` | build/test/run/publish, pre-release checklist, dependency policy |
| `docs/RELEASE_NOTES.md` | shipped change history |
| `AGENTS.md` | house style: naming, formatting, layer rules, CI caveats |

## Stack and layout

```
src/
  HomeCharts.App/            Avalonia shell + composition root (CompositionRoot.cs, Program.cs)
  HomeCharts.Presentation/   view models, UI state, commands (Mvp/)
  HomeCharts.Application/    use cases and orchestration (UseCases/, Import/)
  HomeCharts.Domain/         entities + deterministic categorization (Model/, Categorization/)
  HomeCharts.Infrastructure/ SQLite persistence + migrations (Persistence/Sqlite/Migrations/)
  HomeCharts.Contracts/      repository + operation interfaces (Operations/, Persistence/)
tests/
  HomeCharts.Domain.Tests/
  HomeCharts.Application.Tests/
  HomeCharts.Infrastructure.Tests/
build/                       run-app.ps1, build-and-run-app.ps1, publish-win-x64.ps1, validate-production.ps1
scripts/run_dev.py           cross-platform dev launcher (mirrors build/run-app.ps1)
docs/                        PROJECT_GUIDE.md, TECHNICAL_DESIGN.md, OPERATIONS.md, RELEASE_NOTES.md, README.md
BankRecipes/*.txt            sample/real bank statement files, the import format's ground truth
```

## Commands

- Restore: `dotnet restore HomeCharts.sln`
- Build (solution): `dotnet build HomeCharts.sln`
- Build (App project, catches malformed XAML solution build misses): `dotnet build src/HomeCharts.App/HomeCharts.App.csproj`
- Test: `dotnet test HomeCharts.sln --no-build`
- Run: `dotnet run --project src/HomeCharts.App/HomeCharts.App.csproj -c Debug`, or `build/run-app.ps1`, or `python scripts/run_dev.py`
- Publish: `pwsh ./build/publish-win-x64.ps1`
- Full local gate (Release restore + App build + solution build + all three test projects + publish): `pwsh ./build/validate-production.ps1`
- CI (`migration-ci.yml`, Release config, windows-latest): restore, build, test, publish — same shape as `validate-production.ps1`, run remotely

## Notes

- `.github/copilot-instructions.md` has been removed: its content (bank file
  format, categorization rules, migration hygiene) is fully absorbed into
  `AGENTS.md`, which predates it as this repo's actual house-style source.
  Nothing in this repo still reads the Copilot file.
- The repo's `.gitignore` excludes only `.idea` and `.vs` — `bin/`, `obj/` and
  `artifacts/` are currently tracked in git. Running `dotnet build`/`dotnet
  test` will dirty those paths regardless of what you actually changed; do not
  mistake that diff noise for your own edit when reporting what changed.
- `.claude/worktrees/` holds an existing git worktree (`claude/ecstatic-jemison-7919b9`)
  unrelated to this setup — left as-is.
