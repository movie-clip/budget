# Capability pack: docs — project `budget`

For the `docs-engineer` lane. Owns `docs/**`, and — at close-out only, when a
lane emitted `pack-corrections.md` — `agentic/capabilities/**`.

---

## Index

Read this block first. You are not expected to read this file end to end — read
what your order touches. Reading one extra section is cheap; acting on a
convention you never read is not.

**Always read:** **Where things live** · **Read the diff, not the story** · **Definition of done for this lane**

| Section | Read it when |
|---|---|
| Close-out order | your order is a `close` phase dispatch applying `pack-corrections.md` |

---

## Where things live

| Doc | Update when |
|---|---|
| `docs/PROJECT_GUIDE.md` | a user-facing flow or the category model changed |
| `docs/TECHNICAL_DESIGN.md` | architecture, a use case, or the data model changed |
| `docs/OPERATIONS.md` | build/test/run/publish steps, the pre-release checklist, or dependency policy changed |
| `docs/RELEASE_NOTES.md` | any shipped change — append, do not rewrite history |

`AGENTS.md` § "Persistence And SQL" and the guardrails already state the
migration/schema rules — this pack does not restate them, only where the
resulting behavior gets documented.

## Read the diff, not the story

**You have no `Bash`, so you cannot run `git diff` yourself.** You do not need
to: your order's `inputs` name the upstream lanes' reports, and their
`changed:` sections **are** the diff — one line per file, written by the lane
that touched it. If your order names no upstream reports and you genuinely
cannot tell what shipped, that is `status: BLOCKED` — not a guess.

Document what those diffs actually show — the migration file, the use-case's
real output shape, the view model's real behavior — not what the delivery
brief predicted before implementation. A brief describes intent; a diff
describes what shipped, and the two are allowed to have drifted over the run's
replans.

**What not to do:** do not invent a `docs/product/**` or `docs/roadmap/**`
structure to mirror `portfolio`'s or `datoro`'s docs layout. This repo's docs
are four files plus an index; a lane that adds structure the repo doesn't
already have has exceeded its brief.

## Close-out order

When dispatched at `close` with a `pack-corrections.md` to apply:

**How to apply an entry is `protocol/packs.md` § Applying corrections**, which
you read on this order. Work from that file, not from memory of this one.

Leave sections the run's lanes did not touch alone, even if they look thin —
`project.md` § "This profile is new" already names that as expected, not a
finding for this lane to fix unprompted.

## Definition of done for this lane

- [ ] Updated doc matches the actual diff, not the original delivery brief
- [ ] `RELEASE_NOTES.md` appended to, not rewritten
- [ ] No new doc-directory structure invented beyond the existing four files
      plus `docs/README.md`
- [ ] Every `pack_corrections` entry from this run's build lanes applied to
      its named section, and only that section
- [ ] No `src/**` or `tests/**` files touched
