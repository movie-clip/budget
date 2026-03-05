using HomeCharts.Contracts.Persistence;
using HomeCharts.Domain.Model;

namespace HomeCharts.Infrastructure.Persistence.Sqlite;

public sealed class SqliteCategoryRepository(IAppDbConnectionFactory connectionFactory) : ICategoryRepository
{
    private readonly IAppDbConnectionFactory _connectionFactory = connectionFactory;

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = new List<Category>();

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, name, color_hex, is_system, created_utc, updated_utc
            FROM categories
            ORDER BY name;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            categories.Add(new Category
            {
                Id = Guid.Parse(reader.GetString(0)),
                Name = reader.GetString(1),
                ColorHex = reader.GetString(2),
                IsSystem = SqliteMapping.FromBit(reader.GetInt64(3)),
                CreatedUtc = SqliteMapping.FromIsoDateTimeOffset(reader.GetString(4)),
                UpdatedUtc = SqliteMapping.FromIsoDateTimeOffset(reader.GetString(5))
            });
        }

        return categories;
    }

    public async Task UpsertAsync(Category category, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO categories(id, name, color_hex, is_system, created_utc, updated_utc)
            VALUES($id, $name, $colorHex, $isSystem, $createdUtc, $updatedUtc)
            ON CONFLICT(id) DO UPDATE SET
                name = excluded.name,
                color_hex = excluded.color_hex,
                is_system = excluded.is_system,
                updated_utc = excluded.updated_utc;
            """;

        AddParameter(command, "$id", category.Id.ToString("D"));
        AddParameter(command, "$name", category.Name);
        AddParameter(command, "$colorHex", category.ColorHex);
        AddParameter(command, "$isSystem", SqliteMapping.ToBit(category.IsSystem));
        AddParameter(command, "$createdUtc", SqliteMapping.ToIso(category.CreatedUtc));
        AddParameter(command, "$updatedUtc", SqliteMapping.ToIso(category.UpdatedUtc));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM categories WHERE id = $id;";
        AddParameter(command, "$id", categoryId.ToString("D"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
