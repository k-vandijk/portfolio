using Azure;
using Azure.Data.Tables;

namespace Web.Models;

public class TransactionEntity : ITableEntity
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Date { get; set; }
    public string Ticker { get; set; }
    public string Amount { get; set; }
    public string PurchasePrice { get; set; }
    public string TransactionCosts { get; set; }
}