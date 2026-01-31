using Dashboard.Domain.Utils;

namespace Dashboard.Application.Helpers;

public static class PeriodHelper
{
    public static string GetDefaultPeriod(DateOnly? firstTransactionDate = null)
    {
        firstTransactionDate ??= DateOnly.Parse(StaticDetails.FirstTransactionDate);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yearsDifference = today.Year - firstTransactionDate.Value.Year;
        return $"{yearsDifference * 365 + StaticDetails.TickerApiBufferDays}d";
    }

    public static string GetPeriodFromTimeRange(string? timerange, DateOnly? firstTransactionDate = null)
    {
        return timerange?.ToUpperInvariant() switch
        {
            "1W" => $"{7 + StaticDetails.TickerApiBufferDays}d",
            "1M" => $"{31 + StaticDetails.TickerApiBufferDays}d",
            "3M" => $"{31 * 3 + StaticDetails.TickerApiBufferDays}d",
            "YTD" => GetYtdPeriod(),
            "ALL" or _ => GetDefaultPeriod(firstTransactionDate)
        };
    }

    public static string GetPeriodFromYear(int year)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yearsDifference = today.Year - year;
        return $"{yearsDifference * 365 + StaticDetails.TickerApiBufferDays}d";
    }

    public static string GetYtdPeriod()
    {
        var today = DateTime.UtcNow;
        var startOfYear = new DateTime(today.Year, 1, 1);
        var daysDifference = (today - startOfYear).Days;
        return $"{daysDifference + StaticDetails.TickerApiBufferDays}d";
    }
}