namespace HomeCharts.Application.UseCases;

public sealed class CreateCategorizationRulesBatchUseCase(CreateCategorizationRuleUseCase createCategorizationRuleUseCase)
{
    private readonly CreateCategorizationRuleUseCase _createCategorizationRuleUseCase = createCategorizationRuleUseCase;

    public async Task<CreateCategorizationRulesBatchResult> ExecuteAsync(
        IReadOnlyCollection<CreateCategorizationRulesBatchItem> items,
        CancellationToken cancellationToken = default)
    {
        var createdCount = 0;
        var duplicateCount = 0;
        var invalidCount = 0;

        foreach (var item in items)
        {
            var result = await _createCategorizationRuleUseCase.ExecuteAsync(item.Pattern, item.CategoryId, cancellationToken);
            if (result.IsCreated)
            {
                createdCount++;
                continue;
            }

            if (result.IsInvalidInput)
            {
                invalidCount++;
                continue;
            }

            duplicateCount++;
        }

        return new CreateCategorizationRulesBatchResult(createdCount, duplicateCount, invalidCount);
    }
}

public sealed record CreateCategorizationRulesBatchItem(string Pattern, Guid CategoryId);

public sealed record CreateCategorizationRulesBatchResult(int CreatedCount, int DuplicateCount, int InvalidCount);
