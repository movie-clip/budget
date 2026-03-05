namespace HomeCharts.Infrastructure.Persistence.Sqlite;

public sealed record SqliteOptions
{
    public string DatabasePath { get; init; } = "homecharts.db";
    public bool EnableWriteAheadLogging { get; init; } = true;
}
