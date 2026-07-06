using System.Globalization;

namespace HomeCharts.Presentation.Mvp;

public sealed record DashboardTrendRowViewModel(string Month, decimal Income, decimal Expenses, decimal Net)
{
    public string IncomeText => Income.ToString("0.00");
    public string ExpensesText => Expenses.ToString("0.00");
    public string NetText => Net.ToString("0.00");
}

public sealed record DashboardCategoryRowViewModel(string Category, decimal Amount, int Transactions)
{
    public string AmountText => Amount.ToString("0.00");
}

public sealed record DashboardUncategorizedRowViewModel(Guid TransactionId, string Date, string Description, decimal Amount)
{
    public string AmountText => Amount.ToString("0.00");
}

public sealed record DashboardTrendBarViewModel(
    string Month,
    decimal Income,
    decimal Expenses,
    decimal Net,
    double IncomePercent,
    double ExpensesPercent)
{
    public string IncomeText => Income.ToString("0.00");
    public string ExpensesText => Expenses.ToString("0.00");
    public string NetText => Net.ToString("0.00");
    public string IncomeTooltip => $"Income: EUR {Income:0.00}";
    public string ExpensesTooltip => $"Expenses: EUR {Expenses:0.00}";

    public string MonthLabel
        => DateTime.TryParseExact(Month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed.ToString("MMM", CultureInfo.InvariantCulture)
            : Month;

    public double IncomeBarWidth => 16d;
    public double IncomeBarHeight => IncomePercent <= 0d ? 0d : Math.Max(6d, IncomePercent * 2.4d);
    public double ExpensesBarHeight => ExpensesPercent <= 0d ? 0d : Math.Max(6d, ExpensesPercent * 2.4d);
    public double ExpensesBarWidth => 16d;
}

public sealed class DashboardExpenseCategoryBarViewModel : ObservableObject
{
    private readonly Action<DashboardExpenseCategoryBarViewModel>? _onExpanded;
    private bool _isExpanded;

    public DashboardExpenseCategoryBarViewModel(
        string category,
        decimal amount,
        int transactions,
        double percent,
        IReadOnlyList<DashboardExpenseTransactionViewModel> transactionItems,
        Action<DashboardExpenseCategoryBarViewModel>? onExpanded = null)
    {
        Category = category;
        Amount = amount;
        Transactions = transactions;
        Percent = percent;
        TransactionItems = transactionItems;
        _onExpanded = onExpanded;
    }

    public string Category { get; }
    public decimal Amount { get; }
    public int Transactions { get; }
    public double Percent { get; }
    public IReadOnlyList<DashboardExpenseTransactionViewModel> TransactionItems { get; }

    public string AmountText => Amount.ToString("0.00");

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (!SetProperty(ref _isExpanded, value))
            {
                return;
            }

            if (value)
            {
                _onExpanded?.Invoke(this);
            }
        }
    }
}

public sealed record DashboardExpenseTransactionViewModel(
    Guid TransactionId,
    string BookingDate,
    string Description,
    decimal Amount)
{
    public string AmountText => Amount.ToString("0.00");
}

public sealed record DashboardLargestExpenseViewModel(
    Guid TransactionId,
    string BookingDate,
    string Description,
    string Category,
    decimal Amount)
{
    public string AmountText => Amount.ToString("0.00");
}

public sealed record DashboardRecurringExpenseViewModel(
    string Description,
    string Category,
    decimal AverageAmount,
    decimal LastAmount,
    int Occurrences,
    int DistinctMonths,
    string LastSeen)
{
    public string AverageAmountText => AverageAmount.ToString("0.00");
    public string LastAmountText => LastAmount.ToString("0.00");
    public string InsightText => $"{Occurrences} hits across {DistinctMonths} months";
}
