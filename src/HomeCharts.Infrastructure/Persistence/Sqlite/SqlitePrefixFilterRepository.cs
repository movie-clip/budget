using HomeCharts.Contracts.Persistence;

namespace HomeCharts.Infrastructure.Persistence.Sqlite;

public sealed class SqlitePrefixFilterRepository(IAppDbConnectionFactory connectionFactory) : IPrefixFilterRepository
{
    private readonly IAppDbConnectionFactory _connectionFactory = connectionFactory;

    public async Task<IReadOnlyList<string>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<string>();

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT prefix FROM utility_prefix_filters ORDER BY prefix COLLATE NOCASE;";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(reader.GetString(0));
        }

        return results;
    }

    public async Task AddAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var normalized = prefix.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR IGNORE INTO utility_prefix_filters(id, prefix, created_utc)
            VALUES($id, $prefix, $createdUtc);
            """;

        AddParameter(command, "$id", Guid.NewGuid().ToString("D"));
        AddParameter(command, "$prefix", normalized);
        AddParameter(command, "$createdUtc", SqliteMapping.ToIso(DateTimeOffset.UtcNow));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var normalized = prefix.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM utility_prefix_filters WHERE prefix = $prefix COLLATE NOCASE;";
        AddParameter(command, "$prefix", normalized);

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