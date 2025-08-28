namespace DayIncrease;

internal static class SolsticeData
{
    private static readonly Dictionary<int, (DateTime Summer, DateTime Winter)> SolsticeDataMap = new()
    {
        {
            2024,
            (new DateTime(2024, 7, 20, 20, 51, 0, DateTimeKind.Utc),
                new DateTime(2024, 12, 21, 09, 20, 0, DateTimeKind.Utc))
        },
        {
            2025,
            (new DateTime(2025, 7, 21, 02, 42, 0, DateTimeKind.Utc),
                new DateTime(2025, 12, 21, 15, 03, 0, DateTimeKind.Utc))
        },
        {
            2026,
            (new DateTime(2026, 7, 21, 08, 25, 0, DateTimeKind.Utc),
                new DateTime(2026, 12, 21, 20, 50, 0, DateTimeKind.Utc))
        },
        {
            2027,
            (new DateTime(2027, 7, 21, 14, 11, 0, DateTimeKind.Utc),
                new DateTime(2027, 12, 21, 02, 43, 0, DateTimeKind.Utc))
        },
        {
            2028,
            (new DateTime(2028, 7, 20, 20, 02, 0, DateTimeKind.Utc),
                new DateTime(2028, 12, 21, 08, 20, 0, DateTimeKind.Utc))
        },
        {
            2029,
            (new DateTime(2029, 7, 21, 01, 48, 0, DateTimeKind.Utc),
                new DateTime(2029, 12, 21, 14, 14, 0, DateTimeKind.Utc))
        },
        {
            2030,
            (new DateTime(2030, 7, 21, 07, 31, 0, DateTimeKind.Utc),
                new DateTime(2030, 12, 21, 20, 09, 0, DateTimeKind.Utc))
        }
    };

    internal static (DateTime Summer, DateTime Winter)? GetSolsticeByYear(int year)
    {
        if (SolsticeDataMap.TryGetValue(year, out var solstice)) return solstice;
        return null;
    }
}