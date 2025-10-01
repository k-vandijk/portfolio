using System.Globalization;
using Azure;
using Azure.Data.Tables;
using Dashboard.Application.Interfaces;
using Dashboard.Domain.Models;
using Dashboard.Domain.Utils;
using Microsoft.Extensions.Caching.Memory;

namespace Dashboard.Infrastructure.Services;

public class AzureTableService : IAzureTableService
{
    private readonly TableClient _table;
    private readonly IMemoryCache _cache;

    private const string CacheKey = "transactions";

    public AzureTableService(TableClient table, IMemoryCache cache)
    {
        _table = table;
        _cache = cache;
    }

    public async Task<List<Transaction>> GetTransactionsAsync(string connectionString)
    {
        if (_cache.TryGetValue(CacheKey, out List<Transaction>? cached))
            return cached!;

        var transactionsPageable = _table.QueryAsync<TransactionEntity>(filter: "PartitionKey eq 'transactions'");

        var transactions = new List<Transaction>();
        await foreach (var entity in transactionsPageable)
        {
            transactions.Add(ToModel(entity));
        }

        var orderedTransactions = transactions.OrderBy(t => t.Date).ToList();

        _cache.Set(CacheKey, orderedTransactions, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(StaticDetails.SlidingExpirationMinutes),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(StaticDetails.AbsoluteExpirationMinutes)
        });

        return orderedTransactions;
    }

    public async Task AddTransactionAsync(string connectionString, Transaction transaction)
    {
        var entity = ToEntity(transaction);

        // Add will throw if the RowKey already exists; this is usually what you want for "create"
        await _table.AddEntityAsync(entity);

        // Return generated RowKey back to caller if needed
        transaction.RowKey = entity.RowKey;

        // Invalidate cache
        _cache.Remove(CacheKey);
    }

    public async Task DeleteTransactionAsync(string connectionString, string rowKey)
    {
        if (string.IsNullOrWhiteSpace(rowKey))
            throw new ArgumentException("rowKey is required to delete.");

        // ETag.All = skip concurrency check; if you want optimistic concurrency,
        // fetch entity first and pass its ETag instead.
        await _table.DeleteEntityAsync(StaticDetails.PartitionKey, rowKey, ETag.All);

        // Invalidate cache
        _cache.Remove(CacheKey);
    }

    private static TransactionEntity ToEntity(Transaction t)
    {
        return new TransactionEntity
        {
            PartitionKey = StaticDetails.PartitionKey,
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
        if (decimal.TryParse(input, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var inv))
            return inv;

        // Optionele fallback naar nl-NL voor oude/handmatig ingevoerde data met komma
        if (decimal.TryParse(input, NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("nl-NL"), out var nl))
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