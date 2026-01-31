using Dashboard.Domain.Utils;

namespace Dashboard.Application.Helpers;

public static class PeriodHelper
{
    public static string GetDefaultPeriod(DateOnly? firstTransactionDate = null)
    {
        firstTransactionDate ??= DateOnly.Parse(StaticDetails.FirstTransactionDate);

        // Get the difference in months between the first transaction date and today in years and add 1
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yearsDifference = today.Year - firstTransactionDate.Value.Year;
        return $"{yearsDifference + 1}y";
    }

    /// <summary>
    /// Converts a timerange filter to an API period parameter
    /// </summary>
    /// <param name="timerange">Timerange value (1W, 1M, 3M, YTD, ALL)</param>
    /// <param name="firstTransactionDate">Optional first transaction date for ALL period</param>
    /// <returns>API period parameter (7d, 1mo, 3mo, ytd, or calculated years in 'ny' format)</returns>
    public static string GetPeriodFromTimeRange(string? timerange, DateOnly? firstTransactionDate = null)
    {
        return timerange?.ToUpperInvariant() switch
        {
            "1W" => "7d",
            "1M" => "1mo",
            "3M" => "3mo",
            "YTD" => "ytd",
            "ALL" or _ => GetDefaultPeriod(firstTransactionDate)
        };
    }

    /// <summary>
    /// Gets the API period parameter for a specific year
    /// </summary>
    /// <param name="year">The year to fetch data for</param>
    /// <returns>API period parameter in format 'ny' where n is (today's year - target year + 2)</returns>
    public static string GetPeriodFromYear(int year)
    {
        // Get period that covers the specified year plus some buffer
        // We need to fetch slightly more to ensure we have data for the entire year
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yearsDifference = today.Year - year;
        
        // Add 1 to ensure we have enough data coverage
        return $"{yearsDifference + 2}y";
    }
}