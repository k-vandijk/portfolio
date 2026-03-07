using Kvandijk.Portfolio.Application.Dtos;

namespace Kvandijk.Portfolio.Application.ServiceInterfaces;

public interface IPushNotificationService
{
    Task SendNotificationAsync(PushSubscriptionDto subscription, string title, string body);
}
