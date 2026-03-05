using HomeCharts.Application.Import;
using HomeCharts.Contracts.Persistence;
using HomeCharts.Domain.Categorization;

namespace HomeCharts.Application.UseCases;

public sealed class PreviewParsedCategoryExpensesUseCase(
    IRuleRepository ruleRepository,
    ICategoryRepository categoryRepository,
    BankStatementParser parser)
{
    private readonly IRuleRepository _ruleRepository = ruleRepository;
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly BankStatementParser _parser = parser;

    public async Task<PreviewParsedCategoryExpensesResult> ExecuteAsync(
        string fileContent,
        CancellationToken cancellationToken = default)
    {
        var parse = _parser.Parse(fileContent);
        if (parse.Errors.Count > 0)
        {
            return new PreviewParsedCategoryExpensesResult([], parse.Errors, parse.Warnings);
        }

        var rules = await _ruleRepository.GetActiveAsync(cancellationToken);
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        var categoryNames = categories.ToDictionary(static category => category.Id, static category => category.Name);

        var grouped = parse.Rows
            .Where(static row => row.Amount < 0m)
            .Select(row => new
            {
                Row = row,
                Match = CategorizationEngine.Match(DescriptionNormalizer.Normalize(row.Description), rules)
            })
            .Where(static item => item.Match is not null)
            .GroupBy(
                item => categoryNames.GetValueOrDefault(item.Match!.CategoryId),
                StringComparer.OrdinalIgnoreCase)
            .Where(static group => !string.IsNullOrWhiteSpace(group.Key))
            .Select(static group => new ParsedCategoryExpenseItem(
                group.Key!,
                group.Sum(item => Math.Abs(item.Row.Amount)),
                group.Count()))
            .OrderByDescending(static item => item.ExpenseAmount)
            .ThenBy(static item => item.Category, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new PreviewParsedCategoryExpensesResult(grouped, parse.Errors, parse.Warnings);
    }
}

public sealed record ParsedCategoryExpenseItem(string Category, decimal ExpenseAmount, int Transactions);

public sealed record PreviewParsedCategoryExpensesResult(
    IReadOnlyList<ParsedCategoryExpenseItem> Items,
    IReadOnlyList<BankStatementParseError> Errors,
    IReadOnlyList<BankStatementParseWarning> Warnings);