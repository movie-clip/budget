using HomeCharts.Contracts.Persistence;
using HomeCharts.Domain.Model;

namespace HomeCharts.Infrastructure.Persistence.Sqlite;

public sealed class SqliteManualOverrideRepository(IAppDbConnectionFactory connectionFactory) : IManualOverrideRepository
{
    private readonly IAppDbConnectionFactory _connectionFactory = connectionFactory;

    public async Task AddAsync(ManualOverride manualOverride, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO manual_overrides(id, transaction_id, category_id, reason, created_utc)
            VALUES($id, $transactionId, $categoryId, $reason, $createdUtc);
            """;

        AddParameter(command, "$id", manualOverride.Id.ToString("D"));
        AddParameter(command, "$transactionId", manualOverride.TransactionId.ToString("D"));
        AddParameter(command, "$categoryId", manualOverride.CategoryId.ToString("D"));
        AddParameter(command, "$reason", manualOverride.Reason);
        AddParameter(command, "$createdUtc", SqliteMapping.ToIso(manualOverride.CreatedUtc));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ManualOverride>> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        var results = new List<ManualOverride>();

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, transaction_id, category_id, reason, created_utc
            FROM manual_overrides
            WHERE transaction_id = $transactionId
            ORDER BY created_utc DESC;
            """;
        AddParameter(command, "$transactionId", transactionId.ToString("D"));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ManualOverride
            {
                Id = Guid.Parse(reader.GetString(0)),
                TransactionId = Guid.Parse(reader.GetString(1)),
                CategoryId = Guid.Parse(reader.GetString(2)),
                Reason = reader.IsDBNull(3) ? null : reader.GetString(3),
                CreatedUtc = SqliteMapping.FromIsoDateTimeOffset(reader.GetString(4))
            });
        }

        return results;
    }

    public async Task<int> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM manual_overrides;";
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
