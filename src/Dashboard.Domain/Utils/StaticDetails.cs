namespace Dashboard.Domain.Utils;

public static class StaticDetails
{
    public const string TransactionsTableName = "transactions";
    public const string TransactionsPartitionKey = "transactions";

    public const string PushSubscriptionsTableName = "pushsubscriptions";
    public const string PushSubscriptionsPartitionKey = "pushsubscriptions";

    public const string FirstTransactionDate = "2024-06-06";

    public const int AbsoluteCacheExpirationMinutes = 5;
    public const int SlidingCacheExpirationMinutes = 1;

    public const string SidebarStateCookie = "SidebarState";
    
    /// <summary>
    /// Specifies the number of overlapping days, the data is fetched for.
    /// </summary>
    public const int TickerApiBufferDays = 7;

    /// <summary>
    /// Represents the percentage change in portfolio value that triggers alerts or actions when exceeded.
    /// </summary>
    public const decimal PortfolioChangeThresholdPercent = 3.0m;

    /// <summary>
    /// Specifies the interval, in minutes, at which the portfolio is checked for significant changes in value.
    /// This is used by the PortfolioMonitorService to determine how frequently to perform checks.
    /// </summary>
    public const int PortfolioCheckIntervalMinutes = 60;
}