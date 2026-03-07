using Kvandijk.Portfolio.Domain.Utils;

namespace Kvandijk.Portfolio.Application.Helpers;

public static class PeriodHelper
{
    public static string GetDefaultPeriod(DateOnly? firstTransactionDate = null)
    {
        firstTransactionDate ??= DateOnly.Parse(StaticDetails.FirstTransactionDate);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var days = today.DayNumber - firstTransactionDate.Value.DayNumber + StaticDetails.TickerApiBufferDays;
        return $"{Math.Max(days, StaticDetails.TickerApiBufferDays)}d";
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
        var startOfYear = new DateOnly(year, 1, 1);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var days = today.DayNumber - startOfYear.DayNumber + StaticDetails.TickerApiBufferDays;
        return $"{Math.Max(days, StaticDetails.TickerApiBufferDays)}d";
    }

    public static string GetYtdPeriod()
    {
        var today = DateTime.UtcNow;
        var startOfYear = new DateTime(today.Year, 1, 1);
        var daysDifference = (today - startOfYear).Days;
        return $"{daysDifference + StaticDetails.TickerApiBufferDays}d";
    }
}