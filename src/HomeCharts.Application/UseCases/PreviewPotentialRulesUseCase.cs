using HomeCharts.Application.Import;
using HomeCharts.Contracts.Persistence;
using HomeCharts.Domain.Categorization;

namespace HomeCharts.Application.UseCases;

public sealed class PreviewPotentialRulesUseCase(
    IRuleRepository ruleRepository,
    BankStatementParser parser)
{
    private readonly IRuleRepository _ruleRepository = ruleRepository;
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
        var potentialRows = new List<PotentialRuleRow>();

        foreach (var row in parse.Rows)
        {
            var normalized = DescriptionNormalizer.Normalize(row.Description);
            var match = CategorizationEngine.Match(normalized, rules);
            if (match is null)
            {
                potentialRows.Add(new PotentialRuleRow(
                    row.LineNumber,
                    row.BookingDate,
                    row.Description,
                    row.Amount));
            }
        }

        return new PreviewPotentialRulesResult(potentialRows, parse.Errors, parse.Warnings);
    }
}

public sealed record PotentialRuleRow(
    int LineNumber,
    DateOnly BookingDate,
    string Description,
    decimal Amount);

public sealed record PreviewPotentialRulesResult(
    IReadOnlyList<PotentialRuleRow> PotentialRows,
    IReadOnlyList<BankStatementParseError> Errors,
    IReadOnlyList<BankStatementParseWarning> Warnings);
