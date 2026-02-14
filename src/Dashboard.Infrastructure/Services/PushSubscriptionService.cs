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

public class PushSubscriptionService : IPushSubscriptionService
{
    private readonly TableClient _table;
    private readonly IMemoryCache _cache;

    private const string CacheKey = "pushsubscriptions";

    public PushSubscriptionService(
        [FromKeyedServices(StaticDetails.PushSubscriptionsTableName)] TableClient table,
        IMemoryCache cache)
    {
        _table = table;
        _cache = cache;
    }

    public async Task<List<PushSubscriptionDto>> GetSubscriptionsAsync()
    {
        if (_cache.TryGetValue(CacheKey, out List<PushSubscriptionDto>? cached))
            return cached!;

        var query = _table.QueryAsync<PushSubscriptionEntity>(
            filter: $"PartitionKey eq '{StaticDetails.PushSubscriptionsPartitionKey}'");

        var subscriptions = new List<PushSubscriptionDto>();
        await foreach (var entity in query)
        {
            subscriptions.Add(entity.ToDto());
        }

        _cache.Set(CacheKey, subscriptions, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(StaticDetails.SlidingCacheExpirationMinutes),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(StaticDetails.AbsoluteCacheExpirationMinutes)
        });

        return subscriptions;
    }

    public async Task AddSubscriptionAsync(PushSubscriptionDto subscription)
    {
        // Dedup: check if endpoint already exists
        var existing = _table.QueryAsync<PushSubscriptionEntity>(
            filter: $"PartitionKey eq '{StaticDetails.PushSubscriptionsPartitionKey}' and Endpoint eq '{subscription.Endpoint}'");

        await foreach (var _ in existing)
        {
            // Already exists, skip insert
            return;
        }

        var entity = subscription.ToEntity();
        await _table.AddEntityAsync(entity);

        _cache.Remove(CacheKey);
    }

    public async Task DeleteSubscriptionByEndpointAsync(string endpoint)
    {
        var query = _table.QueryAsync<PushSubscriptionEntity>(
            filter: $"PartitionKey eq '{StaticDetails.PushSubscriptionsPartitionKey}' and Endpoint eq '{endpoint}'");

        await foreach (var entity in query)
        {
            await _table.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, ETag.All);
        }

        _cache.Remove(CacheKey);
    }
}
