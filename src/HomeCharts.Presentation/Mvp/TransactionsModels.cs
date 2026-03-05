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

public sealed record RulesUnmatchedTransactionViewModel(
    Guid Id,
    DateOnly BookingDate,
    string Description,
    decimal Amount,
    string SourceAccount)
{
    public string BookingDateText => BookingDate.ToString("yyyy-MM-dd");
    public string AmountText => Amount.ToString("0.00");
}

public sealed record RulesImportedTransactionViewModel(
    Guid Id,
    DateOnly BookingDate,
    string Description,
    decimal Amount)
{
    public string BookingDateText => BookingDate.ToString("yyyy-MM-dd");
    public string AmountText => Amount.ToString("0.00");
}

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

public sealed class EditableRulesImportedTransactionViewModel : ObservableObject
{
    private string _description;
    private CategoryOptionViewModel? _selectedCategory;

    public EditableRulesImportedTransactionViewModel(
        Guid id,
        DateOnly bookingDate,
        string description,
        decimal amount,
        CategoryOptionViewModel? selectedCategory)
    {
        Id = id;
        BookingDate = bookingDate;
        _description = description;
        Amount = amount;
        _selectedCategory = selectedCategory;
    }

    public Guid Id { get; }
    public DateOnly BookingDate { get; }
    public decimal Amount { get; }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public CategoryOptionViewModel? SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
    }

    public string BookingDateText => BookingDate.ToString("yyyy-MM-dd");
    public string AmountText => Amount.ToString("0.00");
}

public sealed record ParsedCategoryExpenseViewModel(string Category, decimal ExpenseAmount, int Transactions)
{
    public string ExpenseAmountText => ExpenseAmount.ToString("0.00");
}

public sealed record PrefixFilterItemViewModel(string Value);
