using HomeCharts.Contracts.Persistence;

namespace HomeCharts.Application.UseCases;

public sealed class GetPrefixFiltersUseCase(IPrefixFilterRepository prefixFilterRepository)
{
    private readonly IPrefixFilterRepository _prefixFilterRepository = prefixFilterRepository;

    public Task<IReadOnlyList<string>> ExecuteAsync(CancellationToken cancellationToken = default)
        => _prefixFilterRepository.GetAllAsync(cancellationToken);
}

public sealed class AddPrefixFilterUseCase(IPrefixFilterRepository prefixFilterRepository)
{
    private readonly IPrefixFilterRepository _prefixFilterRepository = prefixFilterRepository;

    public async Task<bool> ExecuteAsync(string prefix, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return false;
        }

        await _prefixFilterRepository.AddAsync(prefix, cancellationToken);
        return true;
    }
}

public sealed class RemovePrefixFilterUseCase(IPrefixFilterRepository prefixFilterRepository)
{
    private readonly IPrefixFilterRepository _prefixFilterRepository = prefixFilterRepository;

    public async Task<bool> ExecuteAsync(string prefix, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return false;
        }

        await _prefixFilterRepository.DeleteAsync(prefix, cancellationToken);
        return true;
    }
}