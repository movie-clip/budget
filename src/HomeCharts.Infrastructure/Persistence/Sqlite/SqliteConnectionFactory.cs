using System.Data.Common;
using HomeCharts.Contracts.Persistence;
using Microsoft.Data.Sqlite;

namespace HomeCharts.Infrastructure.Persistence.Sqlite;

public sealed class SqliteConnectionFactory(SqliteOptions options) : IAppDbConnectionFactory
{
    private readonly SqliteOptions _options = options;

    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var databaseDirectory = Path.GetDirectoryName(_options.DatabasePath);
        if (!string.IsNullOrWhiteSpace(databaseDirectory))
        {
            Directory.CreateDirectory(databaseDirectory);
        }

        var connectionStringBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = _options.DatabasePath,
            ForeignKeys = true,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        };

        var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        if (_options.EnableWriteAheadLogging)
        {
            await using var pragmaCommand = connection.CreateCommand();
            pragmaCommand.CommandText = "PRAGMA journal_mode = WAL;";
            await pragmaCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        return connection;
    }
}
