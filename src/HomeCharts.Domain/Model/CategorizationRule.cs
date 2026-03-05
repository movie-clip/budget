namespace HomeCharts.Domain.Model;

public sealed record CategorizationRule
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public string Pattern { get; init; } = string.Empty;
    public RuleMatchType MatchType { get; init; } = RuleMatchType.Contains;
    public int Priority { get; init; }
    public Guid CategoryId { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
