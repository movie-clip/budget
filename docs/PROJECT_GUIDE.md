# HomeCharts Project Guide

## What HomeCharts Is

HomeCharts is a desktop budgeting application built with Avalonia, .NET, and SQLite.

It focuses on:

- importing bank statement files,
- deterministic transaction categorization,
- review and correction workflows,
- dashboard insights for income, expenses, and uncategorized activity.

Legacy WPF code is reference-only for behavior intent. Active implementation is the `src/HomeCharts.*` solution.

## Primary User Flows

### 1) Import and Categorize

1. Open `Import` section.
2. Select a bank statement `.txt` file (BankRecipes format: 7 pipe-delimited columns).
3. App parses rows, applies dedup checks, and evaluates rules.
4. Matched/parsed category summaries and rule suggestions are shown.

### 2) Rule Management

- Potential rules are listed in pageable form.
- Each row supports inline edit + category select + save.
- Rule precedence is deterministic:
  - `Exact > Regex > Contains`
  - then by higher priority
  - then lexical stability.

### 3) Transactions Merge Workflow

1. Open `Transactions` section.
2. Browse file to preview matched transactions.
3. Merge matched rows into budget DB.
4. Dashboard refreshes and shows updated totals.

### 4) Dashboard Review

- KPI cards: income, expenses, net, average monthly savings rate.
- This-month-vs-last-month strip: current-month income/expenses/net alongside the signed delta from the previous month.
- Monthly trend chart: income + expenses bars.
- Needs Attention panel: the transactions still needing a category, with a count, categorized-coverage percentage, and total uncategorized amount, plus a "Review in Ledger" action that jumps to the Ledger tab filtered to uncategorized transactions.
- Expenses panel:
  - shows all non-zero categories,
  - supports per-category expand/collapse,
  - shows category transaction rows in a constrained list.

### 5) Utility Configuration

- Prefix filters can be added/removed.
- Prefix filters clean noisy vendor prefixes for display/matching contexts.

## Category Model

The current canonical category order used in UI flows:

1. None
2. Cafe
3. Shopping
4. Grocery
5. Car
6. Entertainment
7. House
8. Medicine
9. Rent
10. Utility
11. Smoke
12. Transport
13. Services
14. Income
15. Transfer
16. Others

Notes:

- `Income` is also auto-assigned by rule application for positive transactions when no rule match exists.
- `Transfer` exists as an explicit category for transfer transactions.

## Data Expectations

Bank statement rows are expected as:

1. Booking date (`dd/MM/yyyy`)
2. Description
3. Value date (`dd/MM/yyyy`)
4. Amount (negative expense, positive income)
5. Running balance
6. Optional external reference
7. Optional source account/card token

The importer tolerates optional empty fields and noisy uppercase descriptions.

## What to Update When Features Change

When behavior changes, update:

- this guide (`PROJECT_GUIDE.md`) for user-facing flow changes,
- `TECHNICAL_DESIGN.md` for architecture/data contract changes,
- `RELEASE_NOTES.md` for shipped change history.
