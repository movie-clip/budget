namespace HomeCharts.Contracts.Persistence;

public interface IMigrationRunner
{
    Task EnsureCreatedAndMigratedAsync(CancellationToken cancellationToken = default);
}
