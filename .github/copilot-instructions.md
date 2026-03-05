# HomeCharts Migration Guidance

## Product intent

- This project is a migration to Avalonia + .NET + SQLite.
- Old WPF code is reference-only for behavior and UX goals.
- Prefer clean architecture boundaries over legacy compatibility.

## Bank statement data format (BankRecipes)

Bank files in `BankRecipes/*.txt` use pipe-delimited rows with 7 columns:

1. Booking date (`dd/MM/yyyy`)
2. Description / vendor text
3. Value date (`dd/MM/yyyy`)
4. Amount (negative = expense, positive = income)
5. Running balance
6. Optional external reference (can be empty)
7. Optional source account/card token (can be empty)

Example row:

`30/06/2025|COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO|30/06/2025|-20.72|22482.40||5402__7020`

## Categorization expectations

- Categorization must be deterministic.
- Rule precedence target: `Exact > Regex > Contains`.
- Tie-breaking target: higher priority first, then stable lexical rule order.
- Support manual overrides and preserve override history.

## Data quality requirements

- Accept empty optional fields (reference, source account).
- Handle bank-specific uppercase vendor descriptions and noisy prefixes.
- Preserve original description while enabling normalized matching fields.

## Migration hygiene

- No business logic in Avalonia code-behind.
- All schema changes through migration files.
- Tests should include realistic rows copied from `BankRecipes` samples.
