using HomeCharts.Domain.Model;

namespace HomeCharts.Contracts.Persistence;

public interface ITransactionRepository
{
    Task UpsertManyAsync(IReadOnlyCollection<Transaction> transactions, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Transaction>> GetByDateRangeAsync(DateOnly fromInclusive, DateOnly toInclusive, CancellationToken cancellationToken = default);
    Task<Transaction?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task SetCategoryAsync(Guid transactionId, Guid? categoryId, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<string>> GetExistingFingerprintsAsync(IReadOnlyCollection<string> fingerprints, CancellationToken cancellationToken = default);
    Task<int> DeleteAllAsync(CancellationToken cancellationToken = default);
}
