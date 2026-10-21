# Capability pack: testing — project `budget`

For the `test-engineer` lane. The only lane that edits `tests/**`.

Companion sources: `budget/CLAUDE.md`, `budget/AGENTS.md` (house style — this
pack does not repeat its naming conventions).

---

## Index

Read this block first. You are not expected to read this file end to end — read
what your order touches. Reading one extra section is cheap; acting on a
convention you never read is not.

**Always read:** **Running tests** · **Gotchas** · **Definition of done for this lane**

| Section | Read it when |
|---|---|
| Where things live | you are adding a test file, or looking for where one lives |
| Bank-recipe fixtures | your order tests import/categorization behavior |
| Technique skills (mattpocock-skills) | you are making the `tdd` call Definition of done requires, or a failing or flaky test resists your first diagnosis |

---

## Where things live

```
tests/
  HomeCharts.Domain.Tests/          pure domain logic — categorization, matching
  HomeCharts.Application.Tests/     use-case behavior and orchestration
  HomeCharts.Infrastructure.Tests/  SQLite-backed workflows, migration-sensitive behavior
```

Add a test in the project that owns the behavior — a categorization
precedence case goes in `Domain.Tests`, a use-case orchestration case in
`Application.Tests`, a migration or repository case in `Infrastructure.Tests`.

`HomeCharts.Application.Tests.csproj` already project-references
`HomeCharts.Presentation.csproj`. That means `Presentation` view-model
behavior (e.g. `MainWindowViewModel`) is already coverable from
`Application.Tests` — by wiring its constructor dependencies to in-memory
fakes and driving it through its public methods — without a separate
`HomeCharts.Presentation.Tests` project.

## Running tests

- All: `dotnet test HomeCharts.sln --no-build`
- Release, CI-equivalent: `dotnet test HomeCharts.sln --configuration Release --no-build`
- One project: `dotnet test tests/<Project>/<Project>.csproj`
- By fully qualified name: `dotnet test tests/HomeCharts.Application.Tests/HomeCharts.Application.Tests.csproj --filter "FullyQualifiedName~<Name>"`
- By method name: `dotnet test tests/HomeCharts.Infrastructure.Tests/HomeCharts.Infrastructure.Tests.csproj --filter "Name~<Name>"`
- By class: `dotnet test tests/HomeCharts.Domain.Tests/HomeCharts.Domain.Tests.csproj --filter "ClassName=<Namespace>.<ClassName>"`
- When iterating on one test, target the specific test project rather than
  the solution — faster and avoids unrelated rebuild noise.

## Bank-recipe fixtures

- `BankRecipes/*.txt` at the repo root is the real (and sample) data format —
  7-column pipe-delimited rows. Prefer copying realistic rows from there over
  synthetic placeholders (`AGENTS.md` § "Testing Guidance").
- Cover deterministic ordering, duplicate detection, migration behavior,
  manual-override preservation, and parse edge cases (empty optional fields,
  noisy uppercase descriptions) — these are the cases `AGENTS.md` names
  explicitly as required coverage, not nice-to-haves.
- A migration or persistence-behavior change should get at least one
  integration-style test in `Infrastructure.Tests` where practical.

## Technique skills (mattpocock-skills)

Constraints for the `tdd` call Definition of done requires: you still never
touch `src/**` to turn a test green (see Scope discipline in your role
file). Seams for a new test are the ones your order's `inputs` name; don't
pause to ask which seam, that's already decided upstream.

A failing or flaky test resists your first diagnosis: call the Skill tool
with `mattpocock-skills:diagnosing-bugs`. Budget's own
`.claude/settings.json` enables this plugin.

Adapted to this lane's constraints:

- If the defect is in the test itself, fix it — that's your job.
- If the defect is in production code, stop at diagnosis: report it as a
  finding per Scope discipline, and do not patch `src/**`.
- The skill's "ask the user" moments become a `risks` entry instead.
- Any scratch/repro file it has you create is named in `handoff` with its
  `git rm <path>` command — you have no delete tool.
- No commit. Your report is the protocol's report block, not a commit message.

## Gotchas

- **MSTest parallelizes at the method level** (`MSTestSettings.cs`). A test
  that shares mutable state with another (a fixed file path, a static
  counter) can fail intermittently under parallel execution without being
  wrong in isolation — check for shared state before writing off a flaky
  result as unrelated.
- **CI runs tests in `Release` configuration**; local runs commonly default to
  `Debug`. A test that only ran locally has not been verified against what CI
  actually gates on — say so in `verification` if you did not also run the
  `--configuration Release` form.
- **Working-tree noise.** `bin/`, `obj/` and `artifacts/` are tracked in git
  (`project.md` § "Mechanical gates"). Running `dotnet test` will dirty those
  paths — do not list them in `changed:`.

## Definition of done for this lane

- [ ] New/changed tests live in the project that owns the behavior
- [ ] If you chose between mocking and a real dependency, or accepted
      test/production coupling as a tradeoff, you called `mattpocock-skills:tdd`
      first (see § Technique skills).
- [ ] The call happens before you finalize the choice — recorded in
      `skill_calls:`, not only noted in `risks` afterward.
- [ ] Realistic `BankRecipes`-style rows used where the case involves import
      or categorization, not synthetic placeholders
- [ ] Deterministic ordering and tie-breaking asserted explicitly wherever the
      case touches categorization
- [ ] Manual-override preservation asserted wherever the case touches reapply
      behavior
- [ ] Ran the specific test project or filter, not just the whole solution,
      while iterating — and the solution-wide run before reporting done
- [ ] No `src/**` files touched — that is the backend/frontend lane's job
