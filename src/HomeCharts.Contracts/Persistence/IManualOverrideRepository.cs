using HomeCharts.Domain.Model;

namespace HomeCharts.Contracts.Persistence;

public interface IManualOverrideRepository
{
    Task AddAsync(ManualOverride manualOverride, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManualOverride>> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task<int> DeleteAllAsync(CancellationToken cancellationToken = default);
}
