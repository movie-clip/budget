# Capability pack: product — project `budget`

For the `producer` lane. Decides which change a request is, whether it is
already covered, and where "no" lives. Does not write stories or code.

---

## Index

Read this block first. You are not expected to read this file end to end — read
what your order touches. Reading one extra section is cheap; acting on a
convention you never read is not.

**Always read:** **Where the plan lives (it mostly doesn't)** · **What is a story here, and what is not** · **Definition of done for this lane**

| Section | Read it when |
|---|---|
| Finding precedent | you are checking whether a request is already covered |
| The legacy-WPF boundary | a request cites old WPF behavior as the target |
| Guardrails to carry into the brief | your brief touches categorization, overrides, or a migration |

---

## Where the plan lives (it mostly doesn't)

There is no `docs/product/` corpus, no epic tree, no backlog file. The
backlog is whatever the human asks for next. `docs/OPERATIONS.md` §
"Pre-Release Checklist" and § "Dependency Policy" are the closest thing to a
standing set of recurring decisions — check them before writing a brief that
silently re-decides one.

## Finding precedent

Before treating a request as new, check:

- `docs/PROJECT_GUIDE.md` for the user flow and category model as they
  currently stand.
- `docs/TECHNICAL_DESIGN.md` for the use case and data model that already
  exist — a request that sounds new is often "change `BuildDashboardSnapshotUseCase`'s
  output" wearing different words.
- `docs/RELEASE_NOTES.md` for whether the request was already shipped and the
  complaint is actually a bug report.

## What is a story here, and what is not

One use case, one view, one migration with no shipped-code dependency — the
delivery brief is the specification, and `story` does not fire
(`phases.md` § "When the brief is the specification"). Write the brief as if
it will be read directly by the lane that implements it, because it will be.

A story is earned when the change crosses more than one layer's contract with
no existing brief covering the crossing, or touches a migration/`Contracts`
interface that shipped Presentation/App code already depends on. Naming which
of these applies — or that neither does — is part of the brief, not left for
`design` to discover.

## The legacy-WPF boundary

Old WPF code is reference-only for behavior *intent* (`README.md`, `AGENTS.md`
§ "Scope"). A request that says "make it work like the old version" is a
request about the outcome the old version aimed for, not a request to port
its code or match its implementation. A brief should say what outcome is
wanted, not cite WPF internals as the target — the active implementation is
`src/HomeCharts.*`, and nothing in the WPF tree is a contract this system
honors.

## Guardrails to carry into the brief

A brief that touches categorization, manual overrides, or a schema change
should name the relevant guardrail explicitly (`project.md` §§ 1, 2, 4) rather
than leaving `design` or `backend` to notice it. This is a briefing
responsibility, not a decision this lane makes on the guardrail's behalf —
the brief flags it; `backend`/`design` apply it.

## Definition of done for this lane

- [ ] Checked `docs/PROJECT_GUIDE.md`, `docs/TECHNICAL_DESIGN.md` and
      `docs/RELEASE_NOTES.md` for precedent before calling a request new
- [ ] Brief states the outcome, not a WPF-code-level target, when the request
      cites old behavior
- [ ] Brief explicitly says whether this is a single-layer slice or a
      story-earning crossing, and why
- [ ] Any guardrail the brief's scope touches is named in the brief
- [ ] No code or test file touched — that is every other lane's job
