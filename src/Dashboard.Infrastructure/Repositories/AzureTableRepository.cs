using Azure.Data.Tables;
using Dashboard.Domain.Utils;
using Microsoft.Extensions.Caching.Memory;

namespace Dashboard.Infrastructure.Repositories;

public abstract class AzureTableRepository<T> where T : class, ITableEntity, new()
{
    protected TableClient Table { get; }
    private readonly IMemoryCache _cache;
    private string CacheKey => $"repo:{typeof(T).Name}";

    protected AzureTableRepository(TableServiceClient serviceClient, string tableName, IMemoryCache cache)
    {
        Table = serviceClient.GetTableClient(tableName);
        Table.CreateIfNotExists();
        _cache = cache;
    }

    protected void InvalidateCache() => _cache.Remove(CacheKey);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey, out IReadOnlyList<T>? cached))
            return cached!;

        var list = new List<T>();

        await foreach (var entity in Table.QueryAsync<T>(cancellationToken: ct))
            list.Add(entity);

        _cache.Set(CacheKey, (IReadOnlyList<T>)list, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(StaticDetails.SlidingCacheExpirationMinutes),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(StaticDetails.AbsoluteCacheExpirationMinutes)
        });

        return list;
    }

    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
    {
        await Table.AddEntityAsync(entity, ct);
        InvalidateCache();
    }

    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    {
        foreach (var e in entities)
            await AddAsync(e, ct);
    }

    public virtual async Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        await Table.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, cancellationToken: ct);
        InvalidateCache();
    }

    public virtual async Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    {
        foreach (var e in entities)
            await DeleteAsync(e, ct);
    }

    public virtual async Task DeleteByRowKeyAsync(string rowKey, CancellationToken ct = default)
    {
        await foreach (var entity in Table.QueryAsync<T>(e => e.RowKey == rowKey, cancellationToken: ct))
            await DeleteAsync(entity, ct);
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        await Table.UpdateEntityAsync(entity, entity.ETag, cancellationToken: ct);
        InvalidateCache();
    }
}