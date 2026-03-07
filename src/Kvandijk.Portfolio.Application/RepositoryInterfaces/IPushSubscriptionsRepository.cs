using Kvandijk.Portfolio.Domain.Entities;

namespace Kvandijk.Portfolio.Application.RepositoryInterfaces;

public interface IPushSubscriptionsRepository : IAzureTableRepository<PushSubscriptionEntity>
{
}