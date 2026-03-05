namespace HomeCharts.Contracts.Operations;

public interface IDataMaintenanceService
{
    Task BackupDatabaseAsync(string targetFilePath, CancellationToken cancellationToken = default);
    Task RestoreDatabaseAsync(string sourceFilePath, CancellationToken cancellationToken = default);
    string GetDatabasePath();
}
