using HomeCharts.Contracts.Persistence;
using HomeCharts.Domain.Model;

namespace HomeCharts.Application.UseCases;

public sealed class SeedDemoRulesUseCase(
    IRuleRepository ruleRepository,
    ICategoryRepository categoryRepository)
{
    private readonly IRuleRepository _ruleRepository = ruleRepository;
    private readonly ICategoryRepository _categoryRepository = categoryRepository;

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        var categoriesByName = categories.ToDictionary(static category => category.Name, StringComparer.OrdinalIgnoreCase);

        var existingRules = await _ruleRepository.GetActiveAsync(cancellationToken);
        var existingNames = existingRules.Select(static rule => rule.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var templates = new[]
        {
            new RuleTemplate("Exact OpenAI Subscription", "Utilities", RuleMatchType.Exact, "COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO", 10),
            new RuleTemplate("Contains Salary Nomina", "Salary", RuleMatchType.Contains, "NOMINA", 8),
            new RuleTemplate("Contains Taxes", "Taxes", RuleMatchType.Contains, "IMPUESTOS", 8),
            new RuleTemplate("Contains Uber", "Transport", RuleMatchType.Contains, "UBER", 7),
            new RuleTemplate("Regex Supermarket", "Groceries", RuleMatchType.Regex, "(MERCADONA|CARREFOUR|LIDL|ALDI)", 6)
        };

        var created = 0;
        foreach (var template in templates)
        {
            if (existingNames.Contains(template.Name))
            {
                continue;
            }

            if (!categoriesByName.TryGetValue(template.CategoryName, out var category))
            {
                continue;
            }

            var now = DateTimeOffset.UtcNow;
            await _ruleRepository.UpsertAsync(new CategorizationRule
            {
                Id = Guid.NewGuid(),
                Name = template.Name,
                Pattern = template.Pattern,
                MatchType = template.MatchType,
                Priority = template.Priority,
                CategoryId = category.Id,
                IsActive = true,
                CreatedUtc = now,
                UpdatedUtc = now
            }, cancellationToken);

            created++;
        }

        return created;
    }

    private sealed record RuleTemplate(string Name, string CategoryName, RuleMatchType MatchType, string Pattern, int Priority);
}
