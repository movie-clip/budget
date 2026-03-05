using HomeCharts.Domain.Model;

namespace HomeCharts.Contracts.Persistence;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(Category category, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid categoryId, CancellationToken cancellationToken = default);
}
