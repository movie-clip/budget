namespace HomeCharts.Domain.Model;

public sealed record ManualOverride
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid TransactionId { get; init; }
    public Guid CategoryId { get; init; }
    public string? Reason { get; init; }
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
