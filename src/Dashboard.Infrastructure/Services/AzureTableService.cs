using Azure;
using Azure.Data.Tables;
using Dashboard.Application.Dtos;
using Dashboard.Application.Helpers;
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

    public async Task<List<Transaction>> GetTransactionsAsync()
    {
        if (_cache.TryGetValue(CacheKey, out List<Transaction>? cached))
            return cached!;

        var transactionsPageable = _table.QueryAsync<TransactionEntity>(filter: "PartitionKey eq 'transactions'");

        var transactions = new List<Transaction>();
        await foreach (var entity in transactionsPageable)
        {
            transactions.Add(entity.ToModel());
        }

        var orderedTransactions = transactions.OrderBy(t => t.Date).ToList();

        _cache.Set(CacheKey, orderedTransactions, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(StaticDetails.SlidingExpirationMinutes),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(StaticDetails.AbsoluteExpirationMinutes)
        });

        return orderedTransactions;
    }

    public async Task AddTransactionAsync(Transaction transaction)
    {
        var entity = transaction.ToEntity();

        // Add will throw if the RowKey already exists; this is usually what you want for "create"
        await _table.AddEntityAsync(entity);

        // Return generated RowKey back to caller if needed
        transaction.RowKey = entity.RowKey;

        // Invalidate cache
        _cache.Remove(CacheKey);
    }

    public async Task DeleteTransactionAsync(string rowKey)
    {
        if (string.IsNullOrWhiteSpace(rowKey))
            throw new ArgumentException("rowKey is required to delete.");

        // ETag.All = skip concurrency check; if you want optimistic concurrency,
        // fetch entity first and pass its ETag instead.
        await _table.DeleteEntityAsync(StaticDetails.PartitionKey, rowKey, ETag.All);

        // Invalidate cache
        _cache.Remove(CacheKey);
    }
}