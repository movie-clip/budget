# Capability pack: architecture — project `budget`

For the `tech-lead` lane, both modes. DESIGN turns an approved brief into a
technical plan before the engineers start; INTEGRATION reviews their combined
output afterward. Same file, because the seam DESIGN plans is exactly what
INTEGRATION checks held.

---

## Index

Read this block first. You are not expected to read this file end to end — read
what your order touches. Reading one extra section is cheap; acting on a
convention you never read is not.

**Always read:** **The seam** · **Severity calls** · **Escalate rather than decide** · **Technique skills**

| Section | Read it when |
|---|---|
| Design pass checklist | you are running DESIGN |
| Integration review checks | you are running INTEGRATION |

---

## The seam

One process, two directions of dependency the engineers must not cross:

```
Contracts (interfaces)  <--  Domain (rules)  <--  Application (use cases)  <--  Infrastructure (SQLite)
                                                        ^
                                                        |  read by
                                                        |
                              Presentation (view models)  <--  App (shell, composition root)
```

`backend` owns everything left of the use-case line; `frontend` owns
`Presentation`/`App`. The only thing that crosses is a use case's *output
shape* and a `Contracts` interface — never a concrete type from
`Infrastructure` reaching a view model, and never a view model deciding
something a use case should have decided instead.

A migration changes the bottom of this stack; a `Contracts` interface change
is the seam between it and everything above. If a migration changes what a
repository returns, the interface change and the migration are the same
design decision, not two independent ones different lanes can make without
talking.

## Design pass checklist

- [ ] Call the Skill tool with `mattpocock-skills:codebase-design` (see §
      Technique skills for scope constraints) before naming the interface
      below — every DESIGN pass, not only when unsure.
- [ ] Record that call in your report's `skill_calls:` field — not only in
      `handoff` or the orchestrator brief.
- [ ] Name the exact `Contracts` interface(s) that change, and their new
      shape, before `backend` or `frontend` starts.
- [ ] For any migration: what happens to existing rows (backfill, default,
      nothing), stated explicitly — not left implicit in the SQL.
- [ ] Confirm the lane split: which parts are `backend`, which are `frontend`,
      and whether they can build in parallel or one blocks the other (a
      `Contracts` change usually means backend's shape lands before frontend
      can consume it).
- [ ] State whether the categorization engine (`Exact`/`Regex`/`Contains`
      precedence) is touched at all — if yes, this is not a routine slice;
      say so and say why guardrail 1 is still satisfied.
- [ ] Note any new dependency, per `docs/OPERATIONS.md` § "Dependency Policy"
      — a new package needs stated rationale, not silent addition.

## Integration review checks

- [ ] `dotnet build HomeCharts.sln` passes with warnings as errors — no
      suppressed warning without a recorded justification.
- [ ] `dotnet build src/HomeCharts.App/HomeCharts.App.csproj` passes,
      whenever the run touched `Presentation` or `App`.
- [ ] `dotnet test HomeCharts.sln --no-build` passes.
- [ ] Every schema change has a numbered migration; no hand-edited shape.
- [ ] No business logic found in `Presentation` or `App` — spot-check any
      touched view model for a decision that should have been a use-case
      call instead.
- [ ] Categorization precedence and tie-breaking unchanged, unless the brief
      explicitly targets them.
- [ ] Manual-override history preserved through any reapply path the run
      touched.
- [ ] `contract_notes` from `backend` and `frontend` agree with each other —
      the shape one lane says it shipped is the shape the other lane says it
      consumed.
- [ ] Before writing any reuse/consistency judgment into `risks` or the
      orchestrator brief, call `mattpocock-skills:codebase-design` (see §
      Technique skills).
- [ ] This includes citing an existing pattern as precedent for what another
      lane did — not only when unsure.
- [ ] Record that call (or its absence) in `skill_calls:` — the field a
      reader checks against § Reuse and consistency for a contradiction.

## Technique skills (mattpocock-skills)

Reference for the calls Design pass checklist and Integration review checks
already require — this section is scope constraints for `codebase-design`,
not a separate trigger. Budget's own `.claude/settings.json` enables this plugin.

Adapted to this lane's constraints:

- Read only the main `SKILL.md` glossary and principles, not its "Going
  deeper" material.
- Do not open `DESIGN-IT-TWICE.md` — it assumes an `Agent` tool you don't
  have.
- Do not open `DEEPENING.md` — it tells the reader to delete files, which no
  lane here can do.
- This is reference vocabulary: it sharpens your contract and reuse
  judgments — the TECHNICAL PLAN in DESIGN, change requests in INTEGRATION.

## Severity calls

**BLOCKING:**
- A migration with no corresponding numbered file, or a hand-edited schema.
- A view model or `App`-layer file computing a category, a duplicate check,
  or a total instead of reading one from a use case.
- A suppressed build warning with no recorded justification.
- Manual override history dropped or overwritable by an auto-apply path.
- A `Contracts` interface change `backend` shipped that `frontend`'s
  `contract_notes` does not acknowledge (or vice versa) — a silent shape
  mismatch that compiles today and breaks the next change.

**SHOULD_FIX:**
- Missing test coverage for a new use case or migration where
  `Infrastructure.Tests`/`Application.Tests` had an obvious, cheap case to
  add.
- A new dependency added without the rationale note `docs/OPERATIONS.md`
  calls for.
- Documentation (`docs/**`) not updated to match a shipped behavior change.

## Escalate rather than decide

A migration whose backfill strategy affects data already on a user's disk in
more than one plausible way (silently null a column vs. compute a default vs.
require re-import) is not this lane's call to make unilaterally — write the
tradeoff into `risks` and let it surface to the human at the next stop, rather
than picking one and shipping it. The same applies to any change that would
alter what an already-shipped `Contracts` interface returns for existing
callers outside this repo's own UI, if one is ever added — there is none
today, but the moment there is, this rule is why you'd notice before it
mattered.
