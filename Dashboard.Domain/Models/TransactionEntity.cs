using Azure;
using Azure.Data.Tables;

namespace Dashboard.Domain.Models;

public class TransactionEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Date { get; set; } = string.Empty;
    public string Ticker { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string PurchasePrice { get; set; } = string.Empty;
    public string TransactionCosts { get; set; } = string.Empty;
}