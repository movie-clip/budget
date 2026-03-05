namespace HomeCharts.Domain.Model;

public sealed record ImportBatch
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string SourceName { get; init; } = string.Empty;
    public string FileHash { get; init; } = string.Empty;
    public DateTimeOffset ImportedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public int ImportedCount { get; init; }
    public int SkippedCount { get; init; }
}
