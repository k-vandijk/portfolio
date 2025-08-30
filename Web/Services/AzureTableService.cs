using Azure;
using Azure.Data.Tables;
using System.Globalization;
using Web.Models;

namespace Web.Services;

public interface IAzureTableService
{
    List<Transaction> GetTransactions();
    Task AddTransactionAsync(Transaction transaction);
    Task DeleteTransactionAsync(string rowKey);
}

public class AzureTableService : IAzureTableService
{
    private readonly IConfiguration _configuration;

    private const string TableName = "transactions";
    private const string Partition = "transactions";

    public AzureTableService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public List<Transaction> GetTransactions()
    {
        var tableClient = GetTableClient();

        Pageable<TransactionEntity> transactions = tableClient.Query<TransactionEntity>(
            filter: "PartitionKey eq 'transactions'"
        );

        return transactions.Select(ToModel).OrderBy(t => t.Date).ToList();
    }

    public async Task AddTransactionAsync(Transaction transaction)
    {
        var table = GetTableClient();

        var entity = ToEntity(transaction);

        // Add will throw if the RowKey already exists; this is usually what you want for "create"
        await table.AddEntityAsync(entity);

        // Return generated RowKey back to caller if needed
        transaction.RowKey = entity.RowKey;
    }

    public async Task DeleteTransactionAsync(string rowKey)
    {
        if (string.IsNullOrWhiteSpace(rowKey))
            throw new ArgumentException("rowKey is required to delete.");

        var table = GetTableClient();

        // ETag.All = skip concurrency check; if you want optimistic concurrency,
        // fetch entity first and pass its ETag instead.
        await table.DeleteEntityAsync(Partition, rowKey, ETag.All);
    }

    private TableClient GetTableClient()
    {
        string connection = _configuration["Secrets:TransactionsTableConnectionString"]
                            ?? throw new InvalidOperationException("Secrets:TransactionsTableConnectionString is not configured.");

        var client = new TableServiceClient(connection).GetTableClient(TableName);
        client.CreateIfNotExists();
        return client;
    }

    private static TransactionEntity ToEntity(Transaction t)
    {
        return new TransactionEntity
        {
            PartitionKey = Partition,
            RowKey = string.IsNullOrWhiteSpace(t.RowKey) ? Guid.NewGuid().ToString("N") : t.RowKey,
            Date = FormatDate(t.Date),
            Ticker = t.Ticker,
            Amount = FormatDecimal(t.Amount),
            PurchasePrice = FormatDecimal(t.PurchasePrice),
            TransactionCosts = FormatDecimal(t.TransactionCosts),
            ETag = ETag.All
        };
    }

    private static Transaction ToModel(TransactionEntity e)
    {
        return new Transaction
        {
            RowKey = e.RowKey,
            Date = ParseDateOnly(e.Date),
            Ticker = e.Ticker,
            Amount = ParseDecimal(e.Amount),
            PurchasePrice = ParseDecimal(e.PurchasePrice),
            TransactionCosts = ParseDecimal(e.TransactionCosts)
        };
    }

    private static decimal ParseDecimal(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return 0m;

        // We slaan op met InvariantCulture, dus eerst (en eigenlijk: uitsluitend) zo parsen
        if (decimal.TryParse(input, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                             CultureInfo.InvariantCulture, out var inv))
            return inv;

        // Optionele fallback naar nl-NL voor oude/handmatig ingevoerde data met komma
        if (decimal.TryParse(input, NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowLeadingSign,
                             CultureInfo.GetCultureInfo("nl-NL"), out var nl))
            return nl;

        return 0m;
    }

    private static DateOnly ParseDateOnly(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return default;

        if (DateOnly.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;

        if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return DateOnly.FromDateTime(dt);

        return default; // or throw new FormatException($"Invalid date: {input}");
    }

    private static string FormatDate(DateOnly d) =>
        d == default ? "" : d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatDecimal(decimal d) =>
        d.ToString("0.################", CultureInfo.InvariantCulture);
}