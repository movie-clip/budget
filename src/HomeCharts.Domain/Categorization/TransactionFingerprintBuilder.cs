using System.Globalization;

namespace HomeCharts.Domain.Categorization;

public static class TransactionFingerprintBuilder
{
    public static string Build(
        DateOnly bookingDate,
        DateOnly? valueDate,
        string description,
        decimal amount,
        decimal runningBalance,
        string? externalReference,
        string? sourceAccount)
    {
        var normalizedDescription = DescriptionNormalizer.Normalize(description);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{bookingDate:yyyy-MM-dd}|{valueDate:yyyy-MM-dd}|{normalizedDescription}|{amount:G29}|{runningBalance:G29}|{externalReference ?? string.Empty}|{sourceAccount ?? string.Empty}");
    }
}
