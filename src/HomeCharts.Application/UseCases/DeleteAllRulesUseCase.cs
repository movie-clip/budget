using HomeCharts.Contracts.Persistence;

namespace HomeCharts.Application.UseCases;

public sealed class DeleteAllRulesUseCase(IRuleRepository ruleRepository)
{
    private readonly IRuleRepository _ruleRepository = ruleRepository;

    public async Task<DeleteAllRulesResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var rules = await _ruleRepository.GetActiveAsync(cancellationToken);
        foreach (var rule in rules)
        {
            await _ruleRepository.DeleteAsync(rule.Id, cancellationToken);
        }

        return new DeleteAllRulesResult(rules.Count);
    }
}

public sealed record DeleteAllRulesResult(int DeletedCount);
