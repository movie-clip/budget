using HomeCharts.Contracts.Persistence;

namespace HomeCharts.Application.UseCases;

public sealed class BuildDashboardSnapshotUseCase(
    ITransactionRepository transactionRepository,
    ICategoryRepository categoryRepository)
{
    private readonly ITransactionRepository _transactionRepository = transactionRepository;
    private readonly ICategoryRepository _categoryRepository = categoryRepository;

    public async Task<DashboardSnapshot> ExecuteAsync(
        DateOnly fromInclusive,
        DateOnly toInclusive,
        int trendMonths = 12,
        int uncategorizedLimit = 20,
        CancellationToken cancellationToken = default)
    {
        var transactions = await _transactionRepository.GetByDateRangeAsync(fromInclusive, toInclusive, cancellationToken);
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        var categoryNames = categories.ToDictionary(static category => category.Id, static category => category.Name);

        var totalIncome = transactions.Where(static transaction => transaction.Amount > 0).Sum(static transaction => transaction.Amount);
        var totalExpenses = transactions.Where(static transaction => transaction.Amount < 0).Sum(static transaction => Math.Abs(transaction.Amount));
        var netAmount = totalIncome - totalExpenses;
        var uncategorized = transactions.Where(static transaction => transaction.CategoryId is null).ToArray();
        var uncategorizedAmount = uncategorized.Sum(static transaction => Math.Abs(transaction.Amount));

        var trend = BuildTrend(transactions, trendMonths);
        var categoriesBreakdown = BuildCategoryBreakdown(transactions, categoryNames);
        var uncategorizedQueue = uncategorized
            .OrderByDescending(static transaction => transaction.BookingDate)
            .Take(Math.Max(1, uncategorizedLimit))
            .Select(static transaction => new UncategorizedItem(
                transaction.Id,
                transaction.BookingDate,
                transaction.Description,
                transaction.Amount,
                transaction.SourceAccount))
            .ToArray();

        var savingsRate = totalIncome <= 0
            ? 0m
            : Math.Round((netAmount / totalIncome) * 100m, 2, MidpointRounding.AwayFromZero);

        return new DashboardSnapshot(
            new DashboardKpi(
                transactions.Count,
                totalIncome,
                totalExpenses,
                netAmount,
                uncategorized.Length,
                uncategorizedAmount,
                savingsRate),
            trend,
            categoriesBreakdown,
            uncategorizedQueue);
    }

    private static IReadOnlyList<TrendPoint> BuildTrend(IReadOnlyCollection<Domain.Model.Transaction> transactions, int trendMonths)
    {
        var months = Math.Max(1, trendMonths);
        var now = DateOnly.FromDateTime(DateTime.Today);
        var startMonth = new DateOnly(now.Year, now.Month, 1).AddMonths(-(months - 1));

        var grouped = transactions
            .Where(transaction => transaction.BookingDate >= startMonth)
            .GroupBy(static transaction => new { transaction.BookingDate.Year, transaction.BookingDate.Month })
            .ToDictionary(
                group => (group.Key.Year, group.Key.Month),
                group => new
                {
                    Income = group.Where(static transaction => transaction.Amount > 0).Sum(static transaction => transaction.Amount),
                    Expense = group.Where(static transaction => transaction.Amount < 0).Sum(static transaction => Math.Abs(transaction.Amount))
                });

        var result = new List<TrendPoint>(months);
        for (var offset = 0; offset < months; offset++)
        {
            var month = startMonth.AddMonths(offset);
            if (grouped.TryGetValue((month.Year, month.Month), out var values))
            {
                result.Add(new TrendPoint(
                    month,
                    values.Income,
                    values.Expense,
                    values.Income - values.Expense));
            }
            else
            {
                result.Add(new TrendPoint(month, 0m, 0m, 0m));
            }
        }

        return result;
    }

    private static IReadOnlyList<CategoryBreakdownItem> BuildCategoryBreakdown(
        IReadOnlyCollection<Domain.Model.Transaction> transactions,
        IReadOnlyDictionary<Guid, string> categoryNames)
    {
        var grouped = transactions
            .Where(static transaction => transaction.Amount < 0)
            .GroupBy(static transaction => transaction.CategoryId)
            .Select(group =>
            {
                var amount = group.Sum(static transaction => Math.Abs(transaction.Amount));
                var categoryName = group.Key is null
                    ? "(Uncategorized)"
                    : categoryNames.GetValueOrDefault(group.Key.Value, "(Unknown)");

                var categoryTransactions = group
                    .OrderByDescending(static transaction => transaction.BookingDate)
                    .ThenByDescending(static transaction => Math.Abs(transaction.Amount))
                    .Select(static transaction => new CategoryTransactionItem(
                        transaction.Id,
                        transaction.BookingDate,
                        transaction.Description,
                        transaction.Amount,
                        transaction.SourceAccount))
                    .ToArray();

                return new CategoryBreakdownItem(
                    group.Key,
                    categoryName,
                    amount,
                    group.Count(),
                    categoryTransactions);
            })
            .OrderByDescending(static item => item.Amount)
            .ThenBy(static item => item.CategoryName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return grouped;
    }
}

public sealed record DashboardSnapshot(
    DashboardKpi Kpi,
    IReadOnlyList<TrendPoint> MonthlyTrend,
    IReadOnlyList<CategoryBreakdownItem> CategoryBreakdown,
    IReadOnlyList<UncategorizedItem> UncategorizedQueue);

public sealed record DashboardKpi(
    int TotalTransactions,
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal NetAmount,
    int UncategorizedCount,
    decimal UncategorizedAmount,
    decimal SavingsRatePercent);

public sealed record TrendPoint(
    DateOnly Month,
    decimal Income,
    decimal Expenses,
    decimal Net);

public sealed record CategoryBreakdownItem(
    Guid? CategoryId,
    string CategoryName,
    decimal Amount,
    int TransactionCount,
    IReadOnlyList<CategoryTransactionItem> Transactions);

public sealed record CategoryTransactionItem(
    Guid TransactionId,
    DateOnly BookingDate,
    string Description,
    decimal Amount,
    string? SourceAccount);

public sealed record UncategorizedItem(
    Guid TransactionId,
    DateOnly BookingDate,
    string Description,
    decimal Amount,
    string? SourceAccount);
