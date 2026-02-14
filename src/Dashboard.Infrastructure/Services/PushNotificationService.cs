using System.Text.Json;
using Dashboard.Application.Dtos;
using Dashboard.Application.Interfaces;
using WebPush;

namespace Dashboard.Infrastructure.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly WebPushClient _client;
    private readonly VapidDetails _vapidDetails;

    public PushNotificationService()
    {
        _client = new WebPushClient();
        _vapidDetails = new VapidDetails(
            Environment.GetEnvironmentVariable("VAPID_SUBJECT")!,
            Environment.GetEnvironmentVariable("VAPID_PUBLIC_KEY")!,
            Environment.GetEnvironmentVariable("VAPID_PRIVATE_KEY")!);
    }

    public async Task SendNotificationAsync(PushSubscriptionDto subscription, string title, string body)
    {
        var pushSubscription = new PushSubscription(
            subscription.Endpoint,
            subscription.P256dh,
            subscription.Auth);

        var payload = JsonSerializer.Serialize(new { title, body });

        await _client.SendNotificationAsync(pushSubscription, payload, _vapidDetails);
    }
}
