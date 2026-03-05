namespace HomeCharts.Domain.Model;

public sealed record Transaction
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateOnly BookingDate { get; init; }
    public DateOnly? ValueDate { get; init; }
    public string Description { get; init; } = string.Empty;
    public string NormalizedDescription { get; init; } = string.Empty;
    public string TransactionFingerprint { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "EUR";
    public string? SourceAccount { get; init; }
    public string? Counterparty { get; init; }
    public string? ExternalReference { get; init; }
    public Guid? CategoryId { get; init; }
    public bool IsDeleted { get; init; }
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
