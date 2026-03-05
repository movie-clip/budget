using HomeCharts.Contracts.Operations;

namespace HomeCharts.Infrastructure.Persistence.Sqlite;

public sealed class SqliteDataMaintenanceService(SqliteOptions options) : IDataMaintenanceService
{
    private readonly SqliteOptions _options = options;

    public string GetDatabasePath() => _options.DatabasePath;

    public Task BackupDatabaseAsync(string targetFilePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sourceFilePath = _options.DatabasePath;
        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("Database file was not found.", sourceFilePath);
        }

        EnsureDirectory(targetFilePath);
        Copy(sourceFilePath, targetFilePath);
        CopySidecarIfExists(sourceFilePath, targetFilePath, "-wal");
        CopySidecarIfExists(sourceFilePath, targetFilePath, "-shm");

        return Task.CompletedTask;
    }

    public Task RestoreDatabaseAsync(string sourceFilePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("Backup file was not found.", sourceFilePath);
        }

        var destinationFilePath = _options.DatabasePath;
        EnsureDirectory(destinationFilePath);

        Copy(sourceFilePath, destinationFilePath);
        CopySidecarIfExists(sourceFilePath, destinationFilePath, "-wal");
        CopySidecarIfExists(sourceFilePath, destinationFilePath, "-shm");

        return Task.CompletedTask;
    }

    private static void Copy(string sourcePath, string destinationPath)
    {
        File.Copy(sourcePath, destinationPath, overwrite: true);
    }

    private static void CopySidecarIfExists(string sourceFilePath, string destinationFilePath, string suffix)
    {
        var sourceSidecar = sourceFilePath + suffix;
        if (!File.Exists(sourceSidecar))
        {
            return;
        }

        var destinationSidecar = destinationFilePath + suffix;
        File.Copy(sourceSidecar, destinationSidecar, overwrite: true);
    }

    private static void EnsureDirectory(string filePath)
    {
        var folder = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            Directory.CreateDirectory(folder);
        }
    }
}
