using HomeCharts.Domain.Model;

namespace HomeCharts.Contracts.Persistence;

public interface IRuleRepository
{
    Task<IReadOnlyList<CategorizationRule>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(CategorizationRule rule, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid ruleId, CancellationToken cancellationToken = default);
}
