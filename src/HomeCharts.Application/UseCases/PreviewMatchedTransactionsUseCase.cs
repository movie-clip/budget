using HomeCharts.Application.Import;
using HomeCharts.Contracts.Persistence;
using HomeCharts.Domain.Categorization;

namespace HomeCharts.Application.UseCases;

public sealed class PreviewMatchedTransactionsUseCase(
    IRuleRepository ruleRepository,
    ICategoryRepository categoryRepository,
    BankStatementParser parser)
{
    private readonly IRuleRepository _ruleRepository = ruleRepository;
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly BankStatementParser _parser = parser;

    public async Task<PreviewMatchedTransactionsResult> ExecuteAsync(
        string fileContent,
        CancellationToken cancellationToken = default)
    {
        var parse = _parser.Parse(fileContent);
        if (parse.Errors.Count > 0)
        {
            return new PreviewMatchedTransactionsResult([], parse.Errors, parse.Warnings, parse.Metrics, 0);
        }

        var rules = await _ruleRepository.GetActiveAsync(cancellationToken);
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        var categoriesById = categories.ToDictionary(static category => category.Id, static category => category.Name);

        var matchedRows = new List<MatchedTransactionPreviewRow>();
        var unmatchedCount = 0;

        foreach (var row in parse.Rows)
        {
            var normalized = DescriptionNormalizer.Normalize(row.Description);
            var match = CategorizationEngine.Match(normalized, rules);
            if (match is null)
            {
                unmatchedCount++;
                continue;
            }

            matchedRows.Add(new MatchedTransactionPreviewRow(
                row.LineNumber,
                row.BookingDate,
                row.Description,
                row.Amount,
                row.SourceAccount,
                match.CategoryId,
                categoriesById.TryGetValue(match.CategoryId, out var categoryName) ? categoryName : "(Unknown)",
                match.RuleId,
                match.RuleName));
        }

        return new PreviewMatchedTransactionsResult(matchedRows, parse.Errors, parse.Warnings, parse.Metrics, unmatchedCount);
    }
}

public sealed record MatchedTransactionPreviewRow(
    int LineNumber,
    DateOnly BookingDate,
    string Description,
    decimal Amount,
    string? SourceAccount,
    Guid CategoryId,
    string CategoryName,
    Guid RuleId,
    string RuleName);

public sealed record PreviewMatchedTransactionsResult(
    IReadOnlyList<MatchedTransactionPreviewRow> MatchedRows,
    IReadOnlyList<BankStatementParseError> Errors,
    IReadOnlyList<BankStatementParseWarning> Warnings,
    BankStatementParseMetrics Metrics,
    int UnmatchedCount);
