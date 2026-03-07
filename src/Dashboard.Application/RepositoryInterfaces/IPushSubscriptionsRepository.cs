using Dashboard.Domain.Entities;

namespace Dashboard.Application.RepositoryInterfaces;

public interface IPushSubscriptionsRepository : IAzureTableRepository<PushSubscriptionEntity>
{
}