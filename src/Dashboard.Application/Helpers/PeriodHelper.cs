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
}