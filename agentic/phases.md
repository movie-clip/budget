<!-- Profile extension: ORCHESTRATOR only, read at intake.
     `check_report.py` reads the `verify` rows here; see `## Phases` below. -->

# Project phases: `budget`

The phase→lane binding for this project. Read this at intake, alongside
`project.md`. No specialist lane needs it.

## Contents

- **Phases** — the binding table: which lane fills each phase, in what order,
  and the clause that decides whether it fires. Copied into the run ledger.
- **When the brief is the specification** — why the `story` trigger is
  narrower here than a rule with no exception would suggest.
- **Why `design` fires whenever a migration is involved** — the one clause
  that isn't obvious from the table alone.
- **What the gates run** — the command each `verify` row owes.
- **The short run** — what most work in this repo actually looks like.

## Phases

`protocol/orchestrator.md` § 2 defines the phases and § 3 the loop that walks
them. This table is the binding: which lane fills each phase **here**, in what
order, and the clause that decides whether it fires at all. The orchestrator
copies it into the run ledger at intake and records a verdict beside every row.

| Phase | # | Lane | Fires when | Human stop |
|---|---|---|---|---|
| ground-truth | 1 | recon | the request names an existing use case, view model or migration, or rests on an unverified claim about current behavior | — |
| framing | 1 | product | the request is a new use case, or changes what an existing one is *for* rather than how well it does it | **yes** — the brief is relayed and the verdict is the human's |
| specification | 1 | story | framing produced scope no approved story covers, **and** the slice spans more than one layer's contract, or touches a migration/`Contracts` interface already relied on by shipped code | **yes** — the one hard stop |
| design | 1 | design | more than one layer changes together, a `Contracts` interface or use-case shape changes, or a new migration is introduced | — |
| build | 1..n | backend, frontend, test, docs | anything that edits `src/**` (non-test), `tests/**` or `docs/**` | — |
| verify | 1 | integration | any build lane was dispatched | — |
| verify | 2 | review | the run carries a story whose acceptance criteria someone must accept | — |
| close | 1 | docs | always — `docs/**` against what shipped, and `pack-corrections.md` against `capabilities/**` **when a lane emitted one** | — |

### When the brief is the specification

The unit of work in this repo is usually **one use case, one view, or one
migration**, owned by lanes that agree on a contract already stable enough not
to need a ticket. The delivery brief states the outcome and the boundary; the
human approved it at the framing stop; a ticketed story after that is a
dispatch and a second human stop spent restating a decision already made.

Record the row as `not triggered (single-layer slice; the delivery brief in
<nn> is the specification)`, and `review` does not fire either — its clause
asks for a story to accept, and there is none.

**What still fires it:** a change spanning more than one layer's contract with
no brief that already covers the crossing, or any change to a migration or a
`Contracts` interface that shipped Presentation/App code already depends on —
"what breaks for the data already on the user's disk" is exactly the question
a single-layer brief never has to answer, and here it must be answered before
`backend` writes the migration.

### Why `design` fires whenever a migration is involved

A migration is the one change in this repo that a rollback cannot always
undo cleanly — data already persisted under the old shape has to be accounted
for, not just a new column added. `design` exists here to force that
accounting before `backend` writes the `.sql` file, not after: what happens to
existing rows, whether a backfill is needed, and whether `Contracts`'
repository interfaces need to change in step. Treat "just add a migration" as
a `design`-fires case even when nothing else about the slice looks
cross-layer.

### What the gates run

**`integration` runs `dotnet build HomeCharts.sln` and `dotnet test
HomeCharts.sln --no-build`**, plus `dotnet build
src/HomeCharts.App/HomeCharts.App.csproj` whenever the run touched
`HomeCharts.Presentation` or `HomeCharts.App`. Write the exact commands into
the gate's order, not a paraphrase — `project.md` § "Mechanical gates" is
where "why these three" lives.

**`review`** verifies the story's acceptance criteria one by one, and reads
the actual `dotnet test` output the build lanes and `integration` produced for
"does it actually work" — not a lane's claim that it does.

### The short run

Most work here is a short run, and that is the system working rather than the
system being skipped. A new categorization rule, a dashboard formatting fix, a
single migration with no shipped-code dependency — `framing`, `specification`
and `story` all fail their clauses, and the run is `build → verify(integration)
→ close`. Three dispatches, and each false clause written into the ledger with
its reason.

**What makes them true, no matter how small it looks:** any migration that an
existing repository implementation or view model already reads — `design`
fires, per above. Any change to what a use case is *for*, rather than a bug in
how it does it — `framing` fires and the verdict is the human's. Any change
crossing `Presentation`/`App` and `Domain`/`Application`/`Infrastructure` in
the same slice with no existing brief — `specification`'s story clause fires.

**A false clause is re-read every turn.** When a `backend` lane discovers
mid-build that a migration needs a `Contracts` interface change nobody
planned, `design`'s clause has stopped being false: that is a re-plan
(`orchestrator.md` § 3), with a `## Replans` row and a bumped `plan:`.
