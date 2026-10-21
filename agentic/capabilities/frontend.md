# Capability pack: frontend — project `budget`

For the `frontend-engineer` lane. Everything about `src/HomeCharts.Presentation/**`
and `src/HomeCharts.App/**` — the layers that display, not the layers that
decide. Where this contradicts general Avalonia/MVVM instincts, this file
wins.

Companion sources: `budget/CLAUDE.md`, `budget/AGENTS.md` (house style — this
pack does not repeat its naming/formatting conventions in full).

---

## Index

Read this block first. You are not expected to read this file end to end — read
what your order touches. Reading one extra section is cheap; acting on a
convention you never read is not.

**Always read:** **View model conventions** · **Guardrails, in code terms** · **Definition of done for this lane**

| Section | Read it when |
|---|---|
| Layout | you are adding a file, or looking for where one lives |
| Dashboard and category ordering | your order touches the dashboard panel or the category list |
| Diagnosing a stuck bug | your first fix attempt didn't hold |

---

## Layout

```
HomeCharts.Presentation/
  Mvp/            view models, UI state, commands (ObservableObject, SetProperty)
HomeCharts.App/
  App.axaml(.cs)         Avalonia application entry
  MainWindow.axaml(.cs)  main shell
  CompositionRoot.cs     wires concrete repositories/use-cases into MainWindowViewModel — wiring only
  Program.cs             process entry point
```

## View model conventions

Per `AGENTS.md` § "Presentation Layer Guidance":

- `MainWindowViewModel` and related view models own UI state, command
  enablement and presentation formatting — not business decisions.
- Use `ObservableObject` and `SetProperty` for state changes.
- Prefer explicit status text updates for user-visible operations (import
  progress, merge results).
- Formatting helpers that are purely display-focused belong here; anything
  that decides a category, a total, or a validity check does not.

## Dashboard and category ordering

- The canonical category order (`docs/PROJECT_GUIDE.md` § "Category Model") is
  a UI/display concern only — if a use case or seed default reorders
  categories, this lane keeps the presentation order synchronized rather than
  inventing a second ordering.
- Dashboard KPI cards, the monthly trend chart and the expenses panel
  (single-open accordion, hover-highlighted rows, constrained per-category
  transaction list) read from `BuildDashboardSnapshotUseCase`'s read model —
  do not compute totals independently in a view model.

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

## Guardrails, in code terms

**No business logic in `App` or `Presentation` (guardrail 3).**
`CompositionRoot.cs` wires dependencies and nothing else. A view model that
matches a category, decides a duplicate, or recomputes a total instead of
reading it from the use-case result is a guardrail violation, not a
convenience.

**Manual overrides, on the display side (guardrail 2).** A view model that
lets a user set a manual category must route that write through the existing
override use case, never through a code path that could also be reached by
the auto-apply flow, so override history stays attributable.

## Definition of done for this lane

- [ ] No categorization, import, or total-computation logic added to a view
      model or to `App`
- [ ] `CompositionRoot.cs` changes are wiring only
- [ ] Manual category edits route through the existing override use case
- [ ] Category display order stays synchronized with seeded defaults if
      either changed
- [ ] `contract_notes` names any assumption about a use-case output shape
      that the backend lane's own notes did not already confirm
- [ ] No test files touched — that is the test lane
