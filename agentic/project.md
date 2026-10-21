# Project profile: `budget`

Repo: `C:\projects\investments\budget` (HomeCharts)
Bound via `budget/.agentic.json`.

**`CLAUDE.md` is already in your context.** Claude Code loads the repo's
`CLAUDE.md` into every dispatched lane at startup, so the product surface, the
stack, the layer layout, the canonical-doc map and the command list reach you
without a read. Treat it as read, and go to it for anything about the code
this file does not carry. (If it is genuinely not in your context, read it —
that is the one case where it is worth a tool call.)

`budget/AGENTS.md` is a second, deeper source on house style — naming,
formatting, error handling, CI caveats — written before this network existed.
It is not superseded; `CLAUDE.md` points to it and this pack does not repeat
it.

This file carries what `CLAUDE.md` does not: the lane map, the **numbering** a
`REFUSED` cites, and the friction the repo docs do not record. Phase bindings
live in `phases.md`, which the orchestrator reads at intake and no specialist
lane needs.

**This profile is new.** It is grounded in what `AGENTS.md`, the README, the
docs and the repo layout actually establish — not in hard-won incident history
the way `portfolio`'s packs are, because none has accumulated here yet. Expect
thinner gotcha sections than a mature project's packs, and expect
`pack_corrections` to do real work over the first several runs.
`protocol/packs.md` is the loop that closes that gap; use it rather than
treating the current thinness as a finding.

---

## Index

Read this block first. You are not expected to read this file end to end — read
what your order touches. Reading one extra section is cheap; acting on a
convention you never read is not.

**Always read:** **Hard guardrails — no plan or lane may violate these** · **Mechanical gates — never bypass**

| Section | Read it when |
|---|---|
| Delivery model | you are the producer, or shaping a request into work |
| Lane routing | you are choosing which lane owns a piece of work |
| Why `quant` is not bound | you are about to propose a mathematics or forecasting gate |
| Reference | you are a recon or product order checking whether something was already decided |

Elsewhere: **`phases.md`** — which lane fills each phase and the clause that
fires it (orchestrator, at intake). **`CLAUDE.md`** — product, stack, layout,
canonical docs, commands (already in your context).

---

## Hard guardrails — no plan or lane may violate these

1. **Deterministic categorization.** Rule precedence is `Exact > Regex >
   Contains`; ties break by higher priority, then stable lexical rule order.
   Never make a categorization outcome depend on anything nondeterministic.
2. **Manual overrides are never silently overwritten.** Auto-apply and
   rule-reapply flows preserve override history and must not replace a user's
   manual category choice.
3. **No business logic in `HomeCharts.App` or `HomeCharts.Presentation`.**
   Decisions belong in `HomeCharts.Domain` / `HomeCharts.Application`; the
   Avalonia shell and view models format, display and dispatch — they do not
   decide.
4. **Schema changes only through numbered SQL migrations** under
   `src/HomeCharts.Infrastructure/Persistence/Sqlite/Migrations`. No
   hand-edited schema without an accompanying migration file.
5. **Warnings-as-errors is the correctness gate.** Source projects build with
   analyzers and warnings as errors; silencing a warning to get a build green
   is a defect to fix, not a build setting to relax.

An order that requires breaking one of these gets `status: REFUSED` naming the
guardrail. **This is the system's only numbered copy** — `CLAUDE.md` restates
the same five unnumbered, so a session working the repo outside the network
still sees them and a cited number still resolves to exactly one rule.

## Delivery model

**Request → change.** There is no PRD or epic tree in this repo — the backlog
is whatever the human asks for next, plus what `docs/OPERATIONS.md`'s
pre-release checklist and dependency policy surface.

A ticketed story is the exception, not the rule, the same way it is in
`datoro` and `tradingview`. One use case, one view, one migration — the
delivery brief is the specification and `story` does not fire. What earns a
story: a change that crosses more than one layer's contract **and** has no
approved brief already covering it, or a change to a migration or a
`Contracts` interface already relied upon by shipped Presentation/App code,
where "what breaks for the data already on disk" is a question the brief did
not answer. `phases.md` § "When the brief is the specification" carries the
exact clause.

## Lane routing

| Lane | Agent | Pack | Owns |
|---|---|---|---|
| recon | `scout` | — | read-only exploration: which use case/view model/migration already does this |
| product | `producer` | `product.md` | which change this is, whether it is already covered, where "no" lives |
| story | `story-author` | — | drafts a ticketed story on the rare slice that needs one |
| design | `tech-lead` (DESIGN) | `architecture.md` | the cross-layer contract (`Contracts` interfaces, use-case shapes), migrations, the lane split |
| backend | `backend-engineer` | `backend.md` | `src/HomeCharts.Domain/**`, `src/HomeCharts.Application/**`, `src/HomeCharts.Infrastructure/**`, `src/HomeCharts.Contracts/**` (non-test) |
| frontend | `frontend-engineer` | `frontend.md` | `src/HomeCharts.Presentation/**`, `src/HomeCharts.App/**` (non-test) |
| test | `test-engineer` | `testing.md` | `tests/**` |
| docs | `docs-engineer` | `docs.md` | `docs/**`, and — at close-out only — `agentic/capabilities/**` when applying `pack-corrections.md` |
| integration | `tech-lead` (INTEGRATION) | `architecture.md` | engineering gate: PASS / CHANGES_REQUESTED |
| review | `reviewer` | — | acceptance gate: PASS / FAIL — only where a story exists |
| protocol-lint | `protocol-linter` | — | on request only: audits this project's profile and packs, at onboarding or when an order asks. Not a delivery phase |

**`backend`/`frontend` are a layer split, not a client/server split.** This is
a single desktop process with no network seam. `backend` owns the layers that
decide (`Domain`, `Application`, `Infrastructure`, `Contracts`); `frontend`
owns the layers that display (`Presentation`, `App`). Read the lane names as
"the deciding half" and "the displaying half" of one binary.

**No project tool server.** `budget/.mcp.json` does not exist, so no
`mcp__project__*` tools are granted here. Every lane runs the plain `dotnet`
commands `CLAUDE.md` names, under `Bash`. If this repo grows one later,
`protocol/authoring.md` § "The project tool server" and § "Adding a project"
are what to read before wiring it.

## Why `quant` is not bound

Unlike `datoro`, this is not a deferred question — there is no financial
modeling surface here to defer. Categorization is pattern matching (`Exact` /
`Regex` / `Contains`) against a fixed precedence and tie-break rule, and the
dashboard is sums and counts over transactions a human has already
categorized. There is no formula to trace to a source, no valuation, no
trust classification — arithmetic with nothing a `quantitative-researcher`
lane would independently re-derive.

**If that ever changes** — a projected-balance forecast, a spending-trend
model, anything that produces a number beyond a sum or a count — that is the
trigger to revisit, via a `quant` RESEARCH brief before any code, the same way
`datoro` and `tradingview` require it. Until then, changes to categorization
and aggregation logic route through `backend` like any other logic, and their
tests are the only check.

Two gates therefore apply here: `integration`, and `review` when a
story exists. `phases.md`'s
`verify` rows are the authority `check_report.py` reads.

## Mechanical gates — never bypass

- **`dotnet build HomeCharts.sln`** (warnings-as-errors on source projects) is
  the fast local lint/analyzer gate. **`dotnet test HomeCharts.sln
  --no-build`** is the fast local test gate. `AGENTS.md` § "Lint And
  Validation" names both as the minimum before finishing substantial changes.
- **Any Avalonia/XAML change also runs `dotnet build
  src/HomeCharts.App/HomeCharts.App.csproj`** explicitly — the solution build
  and the test suite alone do not reliably catch malformed XAML.
- **`pwsh ./build/validate-production.ps1`** is the fuller local gate: Release
  restore, App-project build, solution build, all three test projects, and a
  publish — the closest local equivalent to what CI (`migration-ci.yml`) runs.
- **Working-tree noise.** A lane's `changed:` list names only the files it
  intentionally edited under `src/`, `tests/`, `docs/`, `build/` or `scripts/`
  — never an incidental `bin/`/`obj/`/`artifacts/` diff its own verification
  command produced.
- **Why:** `budget/.gitignore` excludes only `.idea` and `.vs`, so `bin/`,
  `obj/` and `artifacts/` are tracked in git. Any `dotnet build`/`dotnet
  test`/publish command dirties those paths regardless of what a lane actually
  changed.
- **No agent commits.** The human commits.

## Reference

`budget/docs/OPERATIONS.md` § "Pre-Release Checklist" and § "Dependency
Policy" hold this project's own recurring operational decisions, predating
this network. A `recon` or `product` order investigating whether something
was already decided should check there before treating it as new.
