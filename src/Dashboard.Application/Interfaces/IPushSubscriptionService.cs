using Dashboard.Application.Dtos;

namespace Dashboard.Application.Interfaces;

public interface IPushSubscriptionService
{
    Task<List<PushSubscriptionDto>> GetSubscriptionsAsync();
    Task AddSubscriptionAsync(PushSubscriptionDto subscription);
    Task DeleteSubscriptionByEndpointAsync(string endpoint);
}
