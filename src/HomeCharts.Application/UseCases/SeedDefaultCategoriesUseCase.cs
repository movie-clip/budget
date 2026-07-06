using HomeCharts.Contracts.Persistence;
using HomeCharts.Domain.Model;

namespace HomeCharts.Application.UseCases;

public sealed class SeedDefaultCategoriesUseCase(ICategoryRepository categoryRepository)
{
    private static readonly (string Name, string ColorHex)[] Defaults =
    [
        ("Cafe", "#F97316"),
        ("Shopping", "#EAB308"),
        ("Car", "#06B6D4"),
        ("Entertainment", "#8B5CF6"),
        ("Education", "#0EA5E9"),
        ("Grocery", "#22C55E"),
        ("House", "#0EA5E9"),
        ("Medicine", "#EC4899"),
        ("None", "#94A3B8"),
        ("Income", "#22C55E"),
        ("Transfer", "#10B981"),
        ("Others", "#64748B"),
        ("Rent", "#14B8A6"),
        ("Smoke", "#A855F7"),
        ("Travel", "#14B8A6"),
        ("Transport", "#F59E0B"),
        ("Services", "#3B82F6"),
        ("Utility", "#6366F1")
    ];

    private readonly ICategoryRepository _categoryRepository = categoryRepository;

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _categoryRepository.GetAllAsync(cancellationToken);
        var existingByName = existing
            .GroupBy(static category => category.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var item in Defaults)
        {
            if (existingByName.ContainsKey(item.Name))
            {
                continue;
            }

            var now = DateTimeOffset.UtcNow;
            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = item.Name,
                ColorHex = item.ColorHex,
                IsSystem = true,
                CreatedUtc = now,
                UpdatedUtc = now
            };

            await _categoryRepository.UpsertAsync(category, cancellationToken);
        }
    }
}
