using HomeCharts.Application.UseCases;

namespace HomeCharts.Presentation.Mvp;

public sealed record TransactionDetailViewModel(
    Guid TransactionId,
    string BookingDate,
    string? ValueDate,
    string Description,
    string NormalizedDescription,
    string Category,
    string SourceAccount,
    string ExternalReference,
    string Amount);

public sealed record DatePresetOptionViewModel(LedgerDatePreset Value, string Label);

public sealed record MatchedImportedTransactionViewModel(
    int LineNumber,
    DateOnly BookingDate,
    string Description,
    decimal Amount,
    string SourceAccount,
    string Category,
    string RuleName)
{
    public string BookingDateText => BookingDate.ToString("yyyy-MM-dd");
    public string AmountText => Amount.ToString("0.00");
}

public sealed record ParsedCategoryExpenseViewModel(string Category, decimal ExpenseAmount, int Transactions)
{
    public string ExpenseAmountText => ExpenseAmount.ToString("0.00");
}

public sealed record PrefixFilterItemViewModel(string Value);
