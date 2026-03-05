using HomeCharts.Contracts.Persistence;

namespace HomeCharts.Application.UseCases;

public sealed class GetLedgerEntriesUseCase(
    ITransactionRepository transactionRepository,
    ICategoryRepository categoryRepository)
{
    private readonly ITransactionRepository _transactionRepository = transactionRepository;
    private readonly ICategoryRepository _categoryRepository = categoryRepository;

    public async Task<IReadOnlyList<LedgerEntry>> ExecuteAsync(
        DateOnly fromInclusive,
        DateOnly toInclusive,
        CancellationToken cancellationToken = default)
    {
        var result = await ExecuteAsync(new LedgerQuery(fromInclusive, toInclusive), cancellationToken);
        return result.Items;
    }

    public async Task<LedgerQueryResult> ExecuteAsync(
        LedgerQuery query,
        CancellationToken cancellationToken = default)
    {
        var transactions = await _transactionRepository.GetByDateRangeAsync(query.FromInclusive, query.ToInclusive, cancellationToken);
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        var categoriesById = categories.ToDictionary(static category => category.Id, static category => category.Name);

        var searchText = string.IsNullOrWhiteSpace(query.SearchText)
            ? null
            : query.SearchText.Trim();

        var filtered = transactions.Where(transaction =>
            !query.OnlyUncategorized || transaction.CategoryId is null);

        if (query.TransactionIds is { Count: > 0 } transactionIds)
        {
            filtered = filtered.Where(transaction => transactionIds.Contains(transaction.Id));
        }

        if (!string.IsNullOrWhiteSpace(query.SourceAccount))
        {
            var sourceAccount = query.SourceAccount.Trim();
            filtered = filtered.Where(transaction =>
                Contains(transaction.SourceAccount, sourceAccount));
        }

        if (query.CategoryId is not null)
        {
            filtered = filtered.Where(transaction => transaction.CategoryId == query.CategoryId);
        }

        if (query.MinAmount is not null)
        {
            filtered = filtered.Where(transaction => transaction.Amount >= query.MinAmount.Value);
        }

        if (query.MaxAmount is not null)
        {
            filtered = filtered.Where(transaction => transaction.Amount <= query.MaxAmount.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            filtered = filtered.Where(transaction =>
                Contains(transaction.Description, searchText) ||
                Contains(transaction.NormalizedDescription, searchText) ||
                Contains(transaction.SourceAccount, searchText) ||
                Contains(transaction.ExternalReference, searchText) ||
                (transaction.CategoryId is not null &&
                 categoriesById.TryGetValue(transaction.CategoryId.Value, out var categoryName) &&
                 Contains(categoryName, searchText)));
        }

        var projected = filtered
            .Select(transaction => new LedgerEntry(
                transaction.Id,
                transaction.BookingDate,
                transaction.Description,
                transaction.NormalizedDescription,
                transaction.Amount,
                transaction.SourceAccount,
                transaction.ExternalReference,
                transaction.ValueDate,
                transaction.CategoryId,
                transaction.CategoryId is null
                    ? null
                    : categoriesById.GetValueOrDefault(transaction.CategoryId.Value)))
            .ToArray();

        var availableSourceAccounts = projected
            .Select(static item => item.SourceAccount)
            .Where(static source => !string.IsNullOrWhiteSpace(source))
            .Select(static source => source!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static source => source, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var totalMatched = projected.Length;
        var skip = Math.Max(0, query.Skip);
        var take = query.Take <= 0 ? 200 : query.Take;

        var pageItems = projected
            .Skip(skip)
            .Take(take)
            .ToArray();

        return new LedgerQueryResult(pageItems, totalMatched, availableSourceAccounts);
    }

    private static bool Contains(string? value, string searchText)
        => !string.IsNullOrWhiteSpace(value) && value.Contains(searchText, StringComparison.OrdinalIgnoreCase);
}

public sealed record LedgerQuery(
    DateOnly FromInclusive,
    DateOnly ToInclusive,
    string? SearchText = null,
    bool OnlyUncategorized = false,
    int Skip = 0,
    int Take = 200,
    string? SourceAccount = null,
    Guid? CategoryId = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    LedgerDatePreset DatePreset = LedgerDatePreset.Custom,
    IReadOnlySet<Guid>? TransactionIds = null);

public sealed record LedgerQueryResult(
    IReadOnlyList<LedgerEntry> Items,
    int TotalMatchedCount,
    IReadOnlyList<string> AvailableSourceAccounts);

public sealed record LedgerEntry(
    Guid Id,
    DateOnly BookingDate,
    string Description,
    string NormalizedDescription,
    decimal Amount,
    string? SourceAccount,
    string? ExternalReference,
    DateOnly? ValueDate,
    Guid? CategoryId,
    string? CategoryName);

public enum LedgerDatePreset
{
    Custom = 0,
    Last30Days = 1,
    Last90Days = 2,
    ThisMonth = 3,
    ThisYear = 4,
    Last12Months = 5,
    Last5Years = 6
}
