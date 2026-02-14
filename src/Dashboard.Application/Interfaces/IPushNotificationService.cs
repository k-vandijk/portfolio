using Dashboard.Application.Dtos;

namespace Dashboard.Application.Interfaces;

public interface IPushNotificationService
{
    Task SendNotificationAsync(PushSubscriptionDto subscription, string title, string body);
}
