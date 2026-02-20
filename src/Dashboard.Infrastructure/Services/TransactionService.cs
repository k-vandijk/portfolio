using Azure;
using Azure.Data.Tables;
using Dashboard.Application.Dtos;
using Dashboard.Application.Interfaces;
using Dashboard.Application.Mappers;
using Dashboard.Domain.Models;
using Dashboard.Domain.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Dashboard.Infrastructure.Services;

public class TransactionService : ITransactionService
{
    private readonly TableClient _table;
    private readonly IMemoryCache _cache;

    private const string CacheKey = "transactions";

    public TransactionService([FromKeyedServices(StaticDetails.TransactionsTableName)] TableClient table, IMemoryCache cache)
    {
        _table = table;
        _cache = cache;
    }

    public async Task<List<TransactionDto>> GetTransactionsAsync()
    {
        if (_cache.TryGetValue(CacheKey, out List<TransactionDto>? cached))
            return cached!;

        var transactionsPageable = _table.QueryAsync<TransactionEntity>(filter: "PartitionKey eq 'transactions'");

        var transactions = new List<TransactionDto>();
        await foreach (var entity in transactionsPageable)
        {
            transactions.Add(entity.ToModel());
        }

        var orderedTransactions = transactions.OrderBy(t => t.Date).ToList();

        _cache.Set(CacheKey, orderedTransactions, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(StaticDetails.SlidingCacheExpirationMinutes),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(StaticDetails.AbsoluteCacheExpirationMinutes)
        });

        return orderedTransactions;
    }

    public async Task AddTransactionAsync(TransactionDto transaction)
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
        await _table.DeleteEntityAsync(StaticDetails.TransactionsPartitionKey, rowKey, ETag.All);

        // Invalidate cache
        _cache.Remove(CacheKey);
    }
}