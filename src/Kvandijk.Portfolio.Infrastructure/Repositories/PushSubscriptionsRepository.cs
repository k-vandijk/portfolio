using Azure.Data.Tables;
using Kvandijk.Portfolio.Application.RepositoryInterfaces;
using Kvandijk.Portfolio.Domain.Entities;
using Kvandijk.Portfolio.Domain.Utils;
using Microsoft.Extensions.Caching.Memory;

namespace Kvandijk.Portfolio.Infrastructure.Repositories;

public class PushSubscriptionsRepository : AzureTableRepository<PushSubscriptionEntity>, IPushSubscriptionsRepository
{
    public PushSubscriptionsRepository(TableServiceClient serviceClient, IMemoryCache cache) : base(serviceClient, StaticDetails.PushSubscriptionsTableName, cache)
    {
    }
}