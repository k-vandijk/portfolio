using System.Text.Json;
using Dashboard.Application.Dtos;
using Dashboard.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using WebPush;

namespace Dashboard.Infrastructure.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly WebPushClient _client;
    private readonly VapidDetails _vapidDetails;

    public PushNotificationService(IConfiguration config)
    {
        _client = new WebPushClient();
        _vapidDetails = new VapidDetails(
            config["vapid-subject"],
            config["vapid-public-key"],
            config["vapid-private-key"]);
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
