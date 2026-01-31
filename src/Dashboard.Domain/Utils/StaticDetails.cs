namespace Dashboard.Domain.Utils;

public static class StaticDetails
{
    public const string TableName = "transactions";
    public const string PartitionKey = "transactions";

    public const string FirstTransactionDate = "2024-06-06";

    public const int SlidingExpirationMinutes = 10;
    public const int AbsoluteExpirationMinutes = 60;

    public const string SidebarStateCookie = "SidebarState";

    public const int TickerApiBufferDays = 7;
}