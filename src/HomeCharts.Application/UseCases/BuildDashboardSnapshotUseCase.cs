using HomeCharts.Contracts.Persistence;
using HomeCharts.Domain.Categorization;

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
        var uncategorizedAmount = uncategorized
            .Where(static transaction => transaction.Amount < 0m)
            .Sum(static transaction => Math.Abs(transaction.Amount));
        var anchorDate = transactions.Count == 0
            ? DateOnly.FromDateTime(DateTime.Today)
            : transactions.Max(static transaction => transaction.BookingDate);
        var currentMonthStart = new DateOnly(anchorDate.Year, anchorDate.Month, 1);
        var previousMonthStart = currentMonthStart.AddMonths(-1);

        var trend = BuildTrend(transactions, trendMonths, currentMonthStart);
        var categoriesBreakdown = BuildCategoryBreakdown(transactions, categoryNames);
        var comparison = BuildMonthComparison(transactions, currentMonthStart, previousMonthStart);
        var coverage = BuildCoverage(transactions, uncategorizedAmount);
        var largestExpenses = BuildLargestExpenses(transactions, categoryNames, currentMonthStart);
        var recurringExpenses = BuildRecurringExpenses(transactions, categoryNames);
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

        var savingsRate = BuildAverageMonthlySavingsRate(trend);

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
            uncategorizedQueue,
            comparison,
            coverage,
            largestExpenses,
            recurringExpenses);
    }

    private static MonthComparison BuildMonthComparison(
        IReadOnlyCollection<Domain.Model.Transaction> transactions,
        DateOnly currentMonthStart,
        DateOnly previousMonthStart)
    {
        var current = BuildMonthSummary(transactions, currentMonthStart);
        var previous = BuildMonthSummary(transactions, previousMonthStart);

        return new MonthComparison(
            current,
            previous,
            current.Income - previous.Income,
            current.Expenses - previous.Expenses,
            current.Net - previous.Net);
    }

    private static MonthSummary BuildMonthSummary(IReadOnlyCollection<Domain.Model.Transaction> transactions, DateOnly monthStart)
    {
        var monthEnd = monthStart.AddMonths(1);
        var monthTransactions = transactions
            .Where(transaction => transaction.BookingDate >= monthStart && transaction.BookingDate < monthEnd)
            .ToArray();

        var income = monthTransactions.Where(static transaction => transaction.Amount > 0).Sum(static transaction => transaction.Amount);
        var expenses = monthTransactions.Where(static transaction => transaction.Amount < 0).Sum(static transaction => Math.Abs(transaction.Amount));

        return new MonthSummary(
            monthStart,
            income,
            expenses,
            income - expenses,
            monthTransactions.Length);
    }

    private static decimal BuildAverageMonthlySavingsRate(IReadOnlyList<TrendPoint> trend)
    {
        var monthsWithIncome = trend
            .Where(static point => point.Income > 0m)
            .Select(static point => Math.Round((point.Net / point.Income) * 100m, 2, MidpointRounding.AwayFromZero))
            .ToArray();

        if (monthsWithIncome.Length == 0)
        {
            return 0m;
        }

        return Math.Round(monthsWithIncome.Average(), 2, MidpointRounding.AwayFromZero);
    }

    private static DashboardCoverage BuildCoverage(IReadOnlyCollection<Domain.Model.Transaction> transactions, decimal uncategorizedAmount)
    {
        var categorizedCount = transactions.Count(static transaction => transaction.CategoryId is not null);
        var categorizedPercent = transactions.Count == 0
            ? 0m
            : Math.Round(categorizedCount / (decimal)transactions.Count * 100m, 2, MidpointRounding.AwayFromZero);

        return new DashboardCoverage(
            categorizedCount,
            transactions.Count - categorizedCount,
            categorizedPercent,
            uncategorizedAmount);
    }

    private static IReadOnlyList<LargestExpenseItem> BuildLargestExpenses(
        IReadOnlyCollection<Domain.Model.Transaction> transactions,
        IReadOnlyDictionary<Guid, string> categoryNames,
        DateOnly currentMonthStart,
        int take = 6)
    {
        var currentMonthEnd = currentMonthStart.AddMonths(1);

        return transactions
            .Where(transaction => transaction.Amount < 0m && transaction.BookingDate >= currentMonthStart && transaction.BookingDate < currentMonthEnd)
            .OrderByDescending(static transaction => Math.Abs(transaction.Amount))
            .ThenByDescending(static transaction => transaction.BookingDate)
            .Take(Math.Max(1, take))
            .Select(transaction => new LargestExpenseItem(
                transaction.Id,
                transaction.BookingDate,
                transaction.Description,
                Math.Abs(transaction.Amount),
                transaction.CategoryId is null ? "(Uncategorized)" : categoryNames.GetValueOrDefault(transaction.CategoryId.Value, "(Unknown)"),
                transaction.SourceAccount))
            .ToArray();
    }

    private static IReadOnlyList<RecurringExpenseItem> BuildRecurringExpenses(
        IReadOnlyCollection<Domain.Model.Transaction> transactions,
        IReadOnlyDictionary<Guid, string> categoryNames,
        int take = 6)
    {
        return transactions
            .Where(static transaction => transaction.Amount < 0m)
            .Select(transaction => new
            {
                Transaction = transaction,
                Key = string.IsNullOrWhiteSpace(transaction.NormalizedDescription)
                    ? DescriptionNormalizer.Normalize(transaction.Description)
                    : transaction.NormalizedDescription
            })
            .Where(static item => !string.IsNullOrWhiteSpace(item.Key))
            .GroupBy(static item => item.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var ordered = group
                    .OrderByDescending(static item => item.Transaction.BookingDate)
                    .ThenByDescending(static item => Math.Abs(item.Transaction.Amount))
                    .ToArray();

                var latest = ordered[0].Transaction;
                var distinctMonths = group
                    .Select(static item => (item.Transaction.BookingDate.Year, item.Transaction.BookingDate.Month))
                    .Distinct()
                    .Count();

                return new RecurringExpenseItem(
                    latest.Description,
                    latest.CategoryId is null ? "(Uncategorized)" : categoryNames.GetValueOrDefault(latest.CategoryId.Value, "(Unknown)"),
                    Math.Round(group.Average(static item => Math.Abs(item.Transaction.Amount)), 2, MidpointRounding.AwayFromZero),
                    Math.Abs(latest.Amount),
                    ordered.Length,
                    distinctMonths,
                    latest.BookingDate);
            })
            .Where(static item => item.OccurrenceCount >= 2 && item.DistinctMonthCount >= 2)
            .OrderByDescending(static item => item.AverageAmount)
            .ThenByDescending(static item => item.OccurrenceCount)
            .ThenByDescending(static item => item.LastSeenDate)
            .Take(Math.Max(1, take))
            .ToArray();
    }

    private static IReadOnlyList<TrendPoint> BuildTrend(IReadOnlyCollection<Domain.Model.Transaction> transactions, int trendMonths, DateOnly anchorMonth)
    {
        var months = Math.Max(1, trendMonths);
        var startMonth = anchorMonth.AddMonths(-(months - 1));

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
    IReadOnlyList<UncategorizedItem> UncategorizedQueue,
    MonthComparison CurrentVsPreviousMonth,
    DashboardCoverage Coverage,
    IReadOnlyList<LargestExpenseItem> LargestExpenses,
    IReadOnlyList<RecurringExpenseItem> RecurringExpenses);

public sealed record DashboardKpi(
    int TotalTransactions,
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal NetAmount,
    int UncategorizedCount,
    decimal UncategorizedAmount,
    decimal SavingsRatePercent);

public sealed record MonthSummary(
    DateOnly Month,
    decimal Income,
    decimal Expenses,
    decimal Net,
    int TransactionCount);

public sealed record MonthComparison(
    MonthSummary Current,
    MonthSummary Previous,
    decimal IncomeDelta,
    decimal ExpensesDelta,
    decimal NetDelta);

public sealed record DashboardCoverage(
    int CategorizedCount,
    int UncategorizedCount,
    decimal CategorizedPercent,
    decimal UncategorizedAmount);

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

public sealed record LargestExpenseItem(
    Guid TransactionId,
    DateOnly BookingDate,
    string Description,
    decimal Amount,
    string CategoryName,
    string? SourceAccount);

public sealed record RecurringExpenseItem(
    string Description,
    string CategoryName,
    decimal AverageAmount,
    decimal LastAmount,
    int OccurrenceCount,
    int DistinctMonthCount,
    DateOnly LastSeenDate);
