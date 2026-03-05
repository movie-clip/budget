namespace HomeCharts.Presentation.Mvp;

public sealed record LedgerRowViewModel(
    Guid Id,
    DateOnly BookingDate,
    string Description,
    decimal Amount,
    string Category,
    string SourceAccount)
{
    public string BookingDateText => BookingDate.ToString("yyyy-MM-dd");
    public string AmountText => Amount.ToString("0.00");
}
