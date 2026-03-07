using Dashboard.Application.Dtos;

namespace Dashboard.Application.ServiceInterfaces;

public interface IPushNotificationService
{
    Task SendNotificationAsync(PushSubscriptionDto subscription, string title, string body);
}
