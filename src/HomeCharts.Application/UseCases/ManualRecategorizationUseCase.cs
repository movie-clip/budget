using HomeCharts.Contracts.Persistence;
using HomeCharts.Domain.Model;

namespace HomeCharts.Application.UseCases;

public sealed class ManualRecategorizationUseCase(
    ITransactionRepository transactionRepository,
    IManualOverrideRepository manualOverrideRepository)
{
    private readonly ITransactionRepository _transactionRepository = transactionRepository;
    private readonly IManualOverrideRepository _manualOverrideRepository = manualOverrideRepository;

    public async Task<ManualRecategorizationResult> ExecuteAsync(
        IReadOnlyCollection<Guid> transactionIds,
        Guid categoryId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        if (transactionIds.Count == 0)
        {
            return new ManualRecategorizationResult(0, 0, 0, 0);
        }

        var changed = 0;
        var alreadySameCategory = 0;
        var notFound = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var transactionId in transactionIds.Distinct())
        {
            var transaction = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken);
            if (transaction is null)
            {
                notFound++;
                continue;
            }

            if (transaction.CategoryId == categoryId)
            {
                alreadySameCategory++;
                continue;
            }

            await _transactionRepository.SetCategoryAsync(transactionId, categoryId, cancellationToken);
            await _manualOverrideRepository.AddAsync(new ManualOverride
            {
                Id = Guid.NewGuid(),
                TransactionId = transactionId,
                CategoryId = categoryId,
                Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
                CreatedUtc = now
            }, cancellationToken);

            changed++;
        }

        return new ManualRecategorizationResult(transactionIds.Count, changed, alreadySameCategory, notFound);
    }
}

public sealed record ManualRecategorizationResult(
    int RequestedCount,
    int ChangedCount,
    int AlreadySameCategoryCount,
    int NotFoundCount);
