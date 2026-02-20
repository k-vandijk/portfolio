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
    /// Specifies the interval, in minutes, at which the portfolio is checked for significant changes in value.
    /// This is used by the PortfolioMonitorService to determine how frequently to perform checks.
    /// </summary>
    public const int PortfolioCheckIntervalMinutes = 1;

    /// <summary>
    /// The hour (0-23) when scheduled portfolio notifications should start being sent.
    /// </summary>
    public const int NotificationStartHour = 8;

    /// <summary>
    /// The hour (0-23) when scheduled portfolio notifications should stop being sent.
    /// </summary>
    public const int NotificationEndHour = 20;
}