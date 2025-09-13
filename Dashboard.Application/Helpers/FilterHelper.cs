using System.Globalization;
using Dashboard.Application.Dtos;
using Dashboard.Domain.Models;

namespace Dashboard.Application.Helpers;

public static class FilterHelper
{
    public static readonly string[] TIMERANGES = new[] { "1W", "1M", "3M", "YTD", "ALL" };

    public static List<Transaction> FilterTransactions(List<Transaction> transactions, string? tickers, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        if (!string.IsNullOrWhiteSpace(tickers) && !string.Equals(tickers, "null"))
        {
            var set = tickers
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => t.ToUpperInvariant())
                .ToHashSet();

            transactions = transactions
                .Where(t => !string.IsNullOrWhiteSpace(t.Ticker) && set.Contains(t.Ticker.ToUpperInvariant()))
                .ToList();
        }

        if (startDate.HasValue) transactions = transactions.Where(t => t.Date >= startDate.Value).ToList();
        if (endDate.HasValue) transactions = transactions.Where(t => t.Date <= endDate.Value).ToList();

        return transactions;
    }

    public static (DateOnly, DateOnly) GetMinMaxDatesFromTimeRange(string timerange)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return timerange.ToUpperInvariant() switch
        {
            "1W" => (today.AddDays(-7), today),
            "1M" => (today.AddMonths(-1), today),
            "3M" => (today.AddMonths(-3), today),
            "YTD" => (new DateOnly(today.Year, 1, 1), today),
            _ => (DateOnly.MinValue, today) // ALL or unknown
        };
    }

    public static List<LineChartDataPointDto> FilterLineChartDataPoints(List<LineChartDataPointDto> datapoints, DateOnly startDate, DateOnly endDate)
    {
        return datapoints.Where(p =>
            {
                if (DateTime.TryParseExact(p.Label, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
                {
                    var d = DateOnly.FromDateTime(date);
                    return d >= startDate && d <= endDate;
                }
                return false;
            })
            .ToList();
    }
}
