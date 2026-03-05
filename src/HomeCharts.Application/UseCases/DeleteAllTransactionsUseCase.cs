using HomeCharts.Contracts.Persistence;

namespace HomeCharts.Application.UseCases;

public sealed class DeleteAllTransactionsUseCase(
    IManualOverrideRepository manualOverrideRepository,
    ITransactionRepository transactionRepository,
    IImportBatchRepository importBatchRepository)
{
    private readonly IManualOverrideRepository _manualOverrideRepository = manualOverrideRepository;
    private readonly ITransactionRepository _transactionRepository = transactionRepository;
    private readonly IImportBatchRepository _importBatchRepository = importBatchRepository;

    public async Task<DeleteAllTransactionsResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var deletedOverrides = await _manualOverrideRepository.DeleteAllAsync(cancellationToken);
        var deletedTransactions = await _transactionRepository.DeleteAllAsync(cancellationToken);
        var deletedImportBatches = await _importBatchRepository.DeleteAllAsync(cancellationToken);

        return new DeleteAllTransactionsResult(deletedTransactions, deletedOverrides, deletedImportBatches);
    }
}

public sealed record DeleteAllTransactionsResult(int DeletedTransactions, int DeletedOverrides, int DeletedImportBatches);