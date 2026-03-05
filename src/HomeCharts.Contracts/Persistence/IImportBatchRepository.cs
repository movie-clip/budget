using HomeCharts.Domain.Model;

namespace HomeCharts.Contracts.Persistence;

public interface IImportBatchRepository
{
    Task<ImportBatch?> GetByFileHashAsync(string fileHash, CancellationToken cancellationToken = default);
    Task UpsertAsync(ImportBatch batch, CancellationToken cancellationToken = default);
    Task<int> DeleteAllAsync(CancellationToken cancellationToken = default);
}
