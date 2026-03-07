using Azure.Data.Tables;
using Dashboard.Application.RepositoryInterfaces;
using Dashboard.Domain.Entities;
using Dashboard.Domain.Utils;
using Microsoft.Extensions.Caching.Memory;

namespace Dashboard.Infrastructure.Repositories;

public class PushSubscriptionsRepository : AzureTableRepository<PushSubscriptionEntity>, IPushSubscriptionsRepository
{
    public PushSubscriptionsRepository(TableServiceClient serviceClient, IMemoryCache cache) : base(serviceClient, StaticDetails.PushSubscriptionsTableName, cache)
    {
    }
}