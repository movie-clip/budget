namespace HomeCharts.Contracts.Persistence;

public interface IPrefixFilterRepository
{
    Task<IReadOnlyList<string>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(string prefix, CancellationToken cancellationToken = default);
    Task DeleteAsync(string prefix, CancellationToken cancellationToken = default);
}