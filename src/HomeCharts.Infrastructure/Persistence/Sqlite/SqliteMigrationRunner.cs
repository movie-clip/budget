using System.Globalization;
using System.Reflection;
using HomeCharts.Contracts.Persistence;

namespace HomeCharts.Infrastructure.Persistence.Sqlite;

public sealed class SqliteMigrationRunner(IAppDbConnectionFactory connectionFactory) : IMigrationRunner
{
    private readonly IAppDbConnectionFactory _connectionFactory = connectionFactory;

    public async Task EnsureCreatedAndMigratedAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        await using (var createTableCommand = connection.CreateCommand())
        {
            createTableCommand.CommandText = """
                CREATE TABLE IF NOT EXISTS __schema_migrations (
                    version INTEGER PRIMARY KEY,
                    applied_utc TEXT NOT NULL
                );
                """;
            await createTableCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        var appliedVersions = new HashSet<int>();
        await using (var readVersionsCommand = connection.CreateCommand())
        {
            readVersionsCommand.CommandText = "SELECT version FROM __schema_migrations;";
            await using var reader = await readVersionsCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                appliedVersions.Add(reader.GetInt32(0));
            }
        }

        foreach (var migration in GetEmbeddedMigrations())
        {
            if (appliedVersions.Contains(migration.Version))
            {
                continue;
            }

            await using var dbTransaction = await connection.BeginTransactionAsync(cancellationToken);

            await using (var migrationCommand = connection.CreateCommand())
            {
                migrationCommand.Transaction = dbTransaction;
                migrationCommand.CommandText = migration.Sql;
                await migrationCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var versionCommand = connection.CreateCommand())
            {
                versionCommand.Transaction = dbTransaction;
                versionCommand.CommandText = "INSERT INTO __schema_migrations(version, applied_utc) VALUES($version, $appliedUtc);";

                var versionParameter = versionCommand.CreateParameter();
                versionParameter.ParameterName = "$version";
                versionParameter.Value = migration.Version;
                versionCommand.Parameters.Add(versionParameter);

                var appliedUtcParameter = versionCommand.CreateParameter();
                appliedUtcParameter.ParameterName = "$appliedUtc";
                appliedUtcParameter.Value = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                versionCommand.Parameters.Add(appliedUtcParameter);

                await versionCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await dbTransaction.CommitAsync(cancellationToken);
        }
    }

    private static IReadOnlyList<SqlMigration> GetEmbeddedMigrations()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resources = assembly
            .GetManifestResourceNames()
            .Where(name => name.Contains("Persistence.Sqlite.Migrations", StringComparison.Ordinal) && name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        var migrations = new List<SqlMigration>(resources.Count);
        foreach (var resource in resources)
        {
            var fileName = resource.Split('.').Reverse().Skip(1).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(fileName))
            {
                continue;
            }

            var versionPrefix = fileName.Split('_', 2)[0];
            if (!int.TryParse(versionPrefix, NumberStyles.Integer, CultureInfo.InvariantCulture, out var version))
            {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(resource);
            if (stream is null)
            {
                continue;
            }

            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();
            migrations.Add(new SqlMigration(version, sql));
        }

        return migrations.OrderBy(m => m.Version).ToList();
    }

    private sealed record SqlMigration(int Version, string Sql);
}
