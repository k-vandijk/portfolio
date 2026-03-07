using Azure.Data.Tables;
using Dashboard.Application.RepositoryInterfaces;
using Dashboard.Domain.Entities;
using Dashboard.Domain.Utils;

namespace Dashboard.Infrastructure.Repositories;

public class PushSubscriptionsRepository : AzureTableRepository<PushSubscriptionEntity>, IPushSubscriptionsRepository
{
    public PushSubscriptionsRepository(TableServiceClient serviceClient) : base(serviceClient, StaticDetails.PushSubscriptionsTableName)
    {
    }
}