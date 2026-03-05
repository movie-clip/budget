using System.Globalization;

namespace HomeCharts.Infrastructure.Persistence.Sqlite;

internal static class SqliteMapping
{
    public static string ToIso(DateTimeOffset value) => value.ToString("O", CultureInfo.InvariantCulture);

    public static DateTimeOffset FromIsoDateTimeOffset(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    public static string ToIsoDate(DateOnly value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static DateOnly FromIsoDate(string value) => DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static int ToBit(bool value) => value ? 1 : 0;

    public static bool FromBit(long value) => value == 1;
}
