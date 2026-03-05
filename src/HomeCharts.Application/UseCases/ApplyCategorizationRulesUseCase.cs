using HomeCharts.Contracts.Persistence;
using HomeCharts.Domain.Categorization;

namespace HomeCharts.Application.UseCases;

public sealed class ApplyCategorizationRulesUseCase(
    ITransactionRepository transactionRepository,
    IRuleRepository ruleRepository,
    IManualOverrideRepository manualOverrideRepository,
    ICategoryRepository categoryRepository)
{
    private readonly ITransactionRepository _transactionRepository = transactionRepository;
    private readonly IRuleRepository _ruleRepository = ruleRepository;
    private readonly IManualOverrideRepository _manualOverrideRepository = manualOverrideRepository;
    private readonly ICategoryRepository _categoryRepository = categoryRepository;

    public async Task<ApplyCategorizationResult> ExecuteAsync(
        DateOnly fromInclusive,
        DateOnly toInclusive,
        CancellationToken cancellationToken = default)
    {
        var transactions = await _transactionRepository.GetByDateRangeAsync(fromInclusive, toInclusive, cancellationToken);
        var rules = await _ruleRepository.GetActiveAsync(cancellationToken);
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        var incomeCategoryId = categories
            .FirstOrDefault(category => string.Equals(category.Name, "Income", StringComparison.OrdinalIgnoreCase))
            ?.Id;

        var categorized = 0;
        var skippedManualOverride = 0;
        var noMatch = 0;

        foreach (var transaction in transactions)
        {
            var overrides = await _manualOverrideRepository.GetByTransactionIdAsync(transaction.Id, cancellationToken);
            if (overrides.Count > 0)
            {
                skippedManualOverride++;
                continue;
            }

            var descriptionForMatching = string.IsNullOrWhiteSpace(transaction.NormalizedDescription)
                ? DescriptionNormalizer.Normalize(transaction.Description)
                : transaction.NormalizedDescription;

            var match = CategorizationEngine.Match(descriptionForMatching, rules);
            if (match is null)
            {
                if (transaction.Amount > 0m && incomeCategoryId.HasValue && transaction.CategoryId != incomeCategoryId.Value)
                {
                    await _transactionRepository.SetCategoryAsync(transaction.Id, incomeCategoryId.Value, cancellationToken);
                    categorized++;
                    continue;
                }

                noMatch++;
                continue;
            }

            if (transaction.CategoryId == match.CategoryId)
            {
                continue;
            }

            await _transactionRepository.SetCategoryAsync(transaction.Id, match.CategoryId, cancellationToken);
            categorized++;
        }

        return new ApplyCategorizationResult(
            transactions.Count,
            categorized,
            skippedManualOverride,
            noMatch);
    }
}

public sealed record ApplyCategorizationResult(
    int ProcessedCount,
    int CategorizedCount,
    int SkippedByManualOverrideCount,
    int NoMatchCount);
