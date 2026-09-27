namespace HomeCharts.Application.UseCases;

public sealed record PotentialRuleDraft(
    string SuggestedPattern,
    Guid? SuggestedCategoryId,
    string Pattern,
    Guid? CategoryId)
{
    public static PotentialRuleDraft FromRow(PotentialRuleRow row)
    {
        return new PotentialRuleDraft(
            row.SuggestedPattern,
            row.SuggestedCategoryId,
            row.SuggestedPattern,
            row.SuggestedCategoryId);
    }

    public bool IsModified =>
        !string.Equals(SuggestedPattern, Pattern, StringComparison.Ordinal) || SuggestedCategoryId != CategoryId;

    public bool IsApplicable => IsModified && CategoryId is not null;
}

public enum PotentialRuleSaveOutcome
{
    Created,
    Duplicate,
    InvalidPattern,
    CategoryMissing
}

public sealed record ApplyPotentialRulesResult(int ConsideredCount, int CreatedCount, int DuplicateCount, int InvalidCount);

public sealed class ApplyPotentialRulesUseCase(CreateCategorizationRuleUseCase createRule)
{
    private readonly CreateCategorizationRuleUseCase _createRule = createRule;

    public async Task<PotentialRuleSaveOutcome> SaveOneAsync(
        PotentialRuleDraft draft,
        CancellationToken cancellationToken = default)
    {
        if (draft.CategoryId is not { } categoryId)
        {
            return PotentialRuleSaveOutcome.CategoryMissing;
        }

        var result = await _createRule.ExecuteAsync(draft.Pattern, categoryId, cancellationToken);
        if (result.IsInvalidInput)
        {
            return PotentialRuleSaveOutcome.InvalidPattern;
        }

        return result.IsCreated ? PotentialRuleSaveOutcome.Created : PotentialRuleSaveOutcome.Duplicate;
    }

    public async Task<ApplyPotentialRulesResult> SaveModifiedAsync(
        IReadOnlyCollection<PotentialRuleDraft> drafts,
        CancellationToken cancellationToken = default)
    {
        var considered = 0;
        var created = 0;
        var duplicate = 0;
        var invalid = 0;

        foreach (var draft in drafts.Where(static item => item.IsApplicable))
        {
            considered++;
            switch (await SaveOneAsync(draft, cancellationToken))
            {
                case PotentialRuleSaveOutcome.Created:
                    created++;
                    break;
                case PotentialRuleSaveOutcome.InvalidPattern:
                    invalid++;
                    break;
                default:
                    duplicate++;
                    break;
            }
        }

        return new ApplyPotentialRulesResult(considered, created, duplicate, invalid);
    }
}
