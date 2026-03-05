using HomeCharts.Contracts.Persistence;
using HomeCharts.Domain.Model;

namespace HomeCharts.Application.UseCases;

public sealed class CreateCategorizationRuleUseCase(IRuleRepository ruleRepository)
{
    private readonly IRuleRepository _ruleRepository = ruleRepository;

    public async Task<CreateCategorizationRuleResult> ExecuteAsync(
        string pattern,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return new CreateCategorizationRuleResult(false, true, null);
        }

        var normalizedPattern = pattern.Trim();
        var activeRules = await _ruleRepository.GetActiveAsync(cancellationToken);
        var duplicate = activeRules.FirstOrDefault(rule =>
            rule.CategoryId == categoryId &&
            rule.MatchType == RuleMatchType.Contains &&
            string.Equals(rule.Pattern, normalizedPattern, StringComparison.OrdinalIgnoreCase));

        if (duplicate is not null)
        {
            return new CreateCategorizationRuleResult(false, false, duplicate.Id);
        }

        var now = DateTimeOffset.UtcNow;
        var newRule = new CategorizationRule
        {
            Id = Guid.NewGuid(),
            Name = BuildRuleName(normalizedPattern),
            Pattern = normalizedPattern,
            MatchType = RuleMatchType.Contains,
            Priority = 5,
            CategoryId = categoryId,
            IsActive = true,
            CreatedUtc = now,
            UpdatedUtc = now
        };

        await _ruleRepository.UpsertAsync(newRule, cancellationToken);
        return new CreateCategorizationRuleResult(true, false, newRule.Id);
    }

    private static string BuildRuleName(string pattern)
    {
        var compact = string.Join(' ', pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (compact.Length <= 40)
        {
            return $"Contains {compact}";
        }

        return $"Contains {compact[..40]}";
    }
}

public sealed record CreateCategorizationRuleResult(bool IsCreated, bool IsInvalidInput, Guid? RuleId);