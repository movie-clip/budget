using System.Globalization;
using HomeCharts.Application.Import;
using HomeCharts.Contracts.Persistence;
using HomeCharts.Domain.Categorization;
using HomeCharts.Domain.Model;

namespace HomeCharts.Application.UseCases;

public sealed class PreviewPotentialRulesUseCase(
    IRuleRepository ruleRepository,
    ICategoryRepository categoryRepository,
    IPrefixFilterRepository prefixFilterRepository,
    BankStatementParser parser)
{
    private readonly IRuleRepository _ruleRepository = ruleRepository;
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly IPrefixFilterRepository _prefixFilterRepository = prefixFilterRepository;
    private readonly BankStatementParser _parser = parser;

    public async Task<PreviewPotentialRulesResult> ExecuteAsync(
        string fileContent,
        CancellationToken cancellationToken = default)
    {
        var parse = _parser.Parse(fileContent);
        if (parse.Errors.Count > 0)
        {
            return new PreviewPotentialRulesResult([], parse.Errors, parse.Warnings);
        }

        var rules = await _ruleRepository.GetActiveAsync(cancellationToken);
        var prefixes = await _prefixFilterRepository.GetAllAsync(cancellationToken);
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        var defaultCategoryId = ChooseDefaultCategoryId(categories);
        var potentialRows = new List<PotentialRuleRow>();

        foreach (var row in parse.Rows)
        {
            var normalized = DescriptionNormalizer.Normalize(row.Description);
            var match = CategorizationEngine.Match(normalized, rules);
            if (match is null)
            {
                potentialRows.Add(new PotentialRuleRow(
                    BuildRowKey(row.LineNumber, row.BookingDate, row.Amount, row.Description),
                    row.LineNumber,
                    row.BookingDate,
                    row.Description,
                    row.Amount,
                    DescriptionPrefixStripper.Strip(row.Description, prefixes),
                    defaultCategoryId));
            }
        }

        return new PreviewPotentialRulesResult(potentialRows, parse.Errors, parse.Warnings);
    }

    private static string BuildRowKey(int lineNumber, DateOnly bookingDate, decimal amount, string rawDescription)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{lineNumber}|{bookingDate:yyyy-MM-dd}|{amount}|{rawDescription}");
    }

    private static Guid? ChooseDefaultCategoryId(IReadOnlyList<Category> categories)
    {
        var assignable = ExpenseCategoryCatalog.SelectAssignable(categories, static category => category.Name);

        var none = assignable.FirstOrDefault(static category =>
            string.Equals(category.Name, "None", StringComparison.OrdinalIgnoreCase));
        if (none is not null)
        {
            return none.Id;
        }

        var preferred = assignable.FirstOrDefault(static category =>
            !string.Equals(category.Name, "Uncategorized", StringComparison.OrdinalIgnoreCase));
        var chosen = preferred ?? assignable.FirstOrDefault() ?? categories.FirstOrDefault();
        return chosen?.Id;
    }
}

public sealed record PotentialRuleRow(
    string RowKey,
    int LineNumber,
    DateOnly BookingDate,
    string RawDescription,
    decimal Amount,
    string SuggestedPattern,
    Guid? SuggestedCategoryId);

public sealed record PreviewPotentialRulesResult(
    IReadOnlyList<PotentialRuleRow> PotentialRows,
    IReadOnlyList<BankStatementParseError> Errors,
    IReadOnlyList<BankStatementParseWarning> Warnings);
