using System.Globalization;

namespace HomeCharts.Application.Import;

public sealed class BankStatementParser
{
    private static readonly CultureInfo SpanishCulture = new("es-ES");

    public BankStatementParseResult Parse(string fileContent)
    {
        var rows = new List<BankStatementRow>();
        var errors = new List<BankStatementParseError>();
        var warnings = new List<BankStatementParseWarning>();

        var totalLineCount = 0;
        var emptyLineCount = 0;
        var incomeCount = 0;
        var expenseCount = 0;
        var zeroAmountCount = 0;

        if (string.IsNullOrWhiteSpace(fileContent))
        {
            return new BankStatementParseResult(rows, errors, warnings, BankStatementParseMetrics.Empty);
        }

        var lines = fileContent
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n');

        for (var index = 0; index < lines.Length; index++)
        {
            totalLineCount++;
            var rawLine = lines[index];
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                emptyLineCount++;
                continue;
            }

            if (TryParseLine(index + 1, rawLine, out var row, out var errorMessage))
            {
                var parsedRow = row!;
                rows.Add(parsedRow);

                if (parsedRow.Amount > 0)
                {
                    incomeCount++;
                }
                else if (parsedRow.Amount < 0)
                {
                    expenseCount++;
                }
                else
                {
                    zeroAmountCount++;
                }

                warnings.AddRange(GetWarnings(index + 1, parsedRow));
            }
            else
            {
                errors.Add(new BankStatementParseError(index + 1, errorMessage ?? "Invalid row", rawLine));
            }
        }

        var metrics = new BankStatementParseMetrics(
            totalLineCount,
            totalLineCount - emptyLineCount,
            emptyLineCount,
            rows.Count,
            errors.Count,
            warnings.Count,
            incomeCount,
            expenseCount,
            zeroAmountCount,
            0,
            0,
            0);

        return new BankStatementParseResult(rows, errors, warnings, metrics);
    }

    private static IReadOnlyList<BankStatementParseWarning> GetWarnings(int lineNumber, BankStatementRow row)
    {
        var warnings = new List<BankStatementParseWarning>(2);

        if (row.ValueDate < row.BookingDate)
        {
            warnings.Add(new BankStatementParseWarning(
                lineNumber,
                BankStatementWarningCode.ValueDateBeforeBookingDate,
                "Value date is before booking date."));
        }

        if (row.Amount == 0)
        {
            warnings.Add(new BankStatementParseWarning(
                lineNumber,
                BankStatementWarningCode.ZeroAmount,
                "Amount is zero."));
        }

        return warnings;
    }

    private static bool TryParseLine(int lineNumber, string line, out BankStatementRow? row, out string? error)
    {
        row = null;
        error = null;

        var columns = line.Split('|');
        if (columns.Length != 7)
        {
            error = "Expected 7 pipe-delimited columns.";
            return false;
        }

        for (var index = 0; index < columns.Length; index++)
        {
            columns[index] = columns[index].Trim();
        }

        columns[0] = columns[0].TrimStart('\uFEFF');

        if (!DateOnly.TryParseExact(columns[0], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var bookingDate))
        {
            error = "Invalid booking date format (dd/MM/yyyy expected).";
            return false;
        }

        var description = columns[1].Trim();
        if (description.Length == 0)
        {
            error = "Description cannot be empty.";
            return false;
        }

        if (!DateOnly.TryParseExact(columns[2], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var valueDate))
        {
            error = "Invalid value date format (dd/MM/yyyy expected).";
            return false;
        }

        if (!TryParseDecimal(columns[3], out var amount))
        {
            error = "Invalid amount.";
            return false;
        }

        if (!TryParseDecimal(columns[4], out var runningBalance))
        {
            error = "Invalid running balance.";
            return false;
        }

        var externalReference = string.IsNullOrWhiteSpace(columns[5]) ? null : columns[5].Trim();
        var sourceAccount = string.IsNullOrWhiteSpace(columns[6]) ? null : columns[6].Trim();

        row = new BankStatementRow(
            lineNumber,
            bookingDate,
            description,
            valueDate,
            amount,
            runningBalance,
            externalReference,
            sourceAccount);

        return true;
    }

    private static bool TryParseDecimal(string value, out decimal parsed)
    {
        const NumberStyles decimalStyle = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

        return decimal.TryParse(value, decimalStyle, CultureInfo.InvariantCulture, out parsed)
            || decimal.TryParse(value, decimalStyle, SpanishCulture, out parsed);
    }
}

public sealed record BankStatementRow(
    int LineNumber,
    DateOnly BookingDate,
    string Description,
    DateOnly ValueDate,
    decimal Amount,
    decimal RunningBalance,
    string? ExternalReference,
    string? SourceAccount);

public sealed record BankStatementParseError(int LineNumber, string Message, string RawLine);

public enum BankStatementWarningCode
{
    ValueDateBeforeBookingDate = 1,
    ZeroAmount = 2,
    DuplicateFileHash = 3,
    DuplicateRowInFile = 4,
    DuplicateRowInDatabase = 5
}

public sealed record BankStatementParseWarning(int LineNumber, BankStatementWarningCode Code, string Message);

public sealed record BankStatementParseMetrics(
    int TotalLineCount,
    int NonEmptyLineCount,
    int EmptyLineCount,
    int ParsedRowCount,
    int ErrorCount,
    int WarningCount,
    int IncomeCount,
    int ExpenseCount,
    int ZeroAmountCount,
    int DuplicateRowCount,
    int ImportedRowCount,
    int DuplicateExistingRowCount)
{
    public static BankStatementParseMetrics Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}

public sealed record BankStatementParseResult(
    IReadOnlyList<BankStatementRow> Rows,
    IReadOnlyList<BankStatementParseError> Errors,
    IReadOnlyList<BankStatementParseWarning> Warnings,
    BankStatementParseMetrics Metrics);
