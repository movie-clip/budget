using HomeCharts.Contracts.Persistence;

namespace HomeCharts.Application.UseCases;

public sealed class InitializeDatabaseUseCase(IMigrationRunner migrationRunner)
{
    private readonly IMigrationRunner _migrationRunner = migrationRunner;

    public Task ExecuteAsync(CancellationToken cancellationToken = default)
        => _migrationRunner.EnsureCreatedAndMigratedAsync(cancellationToken);
}
