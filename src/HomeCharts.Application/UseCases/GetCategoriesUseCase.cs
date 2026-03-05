using HomeCharts.Contracts.Persistence;

namespace HomeCharts.Application.UseCases;

public sealed class GetCategoriesUseCase(ICategoryRepository categoryRepository)
{
    private readonly ICategoryRepository _categoryRepository = categoryRepository;

    public async Task<IReadOnlyList<CategoryItem>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        return categories
            .OrderBy(static category => category.Name, StringComparer.OrdinalIgnoreCase)
            .Select(static category => new CategoryItem(category.Id, category.Name, category.ColorHex, category.IsSystem))
            .ToArray();
    }
}

public sealed record CategoryItem(Guid Id, string Name, string ColorHex, bool IsSystem);
