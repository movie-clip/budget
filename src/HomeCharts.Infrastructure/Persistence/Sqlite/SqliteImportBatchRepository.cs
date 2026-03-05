using HomeCharts.Contracts.Persistence;
using HomeCharts.Domain.Model;

namespace HomeCharts.Infrastructure.Persistence.Sqlite;

public sealed class SqliteImportBatchRepository(IAppDbConnectionFactory connectionFactory) : IImportBatchRepository
{
    private readonly IAppDbConnectionFactory _connectionFactory = connectionFactory;

    public async Task<ImportBatch?> GetByFileHashAsync(string fileHash, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, source_name, file_hash, imported_at_utc, imported_count, skipped_count
            FROM import_batches
            WHERE file_hash = $fileHash;
            """;
        AddParameter(command, "$fileHash", fileHash);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ImportBatch
        {
            Id = Guid.Parse(reader.GetString(0)),
            SourceName = reader.GetString(1),
            FileHash = reader.GetString(2),
            ImportedAtUtc = SqliteMapping.FromIsoDateTimeOffset(reader.GetString(3)),
            ImportedCount = reader.GetInt32(4),
            SkippedCount = reader.GetInt32(5)
        };
    }

    public async Task UpsertAsync(ImportBatch batch, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO import_batches(id, source_name, file_hash, imported_at_utc, imported_count, skipped_count)
            VALUES($id, $sourceName, $fileHash, $importedAtUtc, $importedCount, $skippedCount)
            ON CONFLICT(id) DO UPDATE SET
                source_name = excluded.source_name,
                file_hash = excluded.file_hash,
                imported_at_utc = excluded.imported_at_utc,
                imported_count = excluded.imported_count,
                skipped_count = excluded.skipped_count;
            """;

        AddParameter(command, "$id", batch.Id.ToString("D"));
        AddParameter(command, "$sourceName", batch.SourceName);
        AddParameter(command, "$fileHash", batch.FileHash);
        AddParameter(command, "$importedAtUtc", SqliteMapping.ToIso(batch.ImportedAtUtc));
        AddParameter(command, "$importedCount", batch.ImportedCount);
        AddParameter(command, "$skippedCount", batch.SkippedCount);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM import_batches;";
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
