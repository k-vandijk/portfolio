using Dashboard.Application.Dtos;
using Dashboard.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard._Web.Controllers;

public class NotificationsController : Controller
{
    private readonly IPushSubscriptionService _subscriptionService;
    private readonly IConfiguration _config;

    public NotificationsController(
        IPushSubscriptionService subscriptionService,
        IConfiguration config)
    {
        _subscriptionService = subscriptionService;
        _config = config;
    }

    [HttpGet("/notifications/vapid-public-key")]
    public IActionResult GetVapidPublicKey()
    {
        var publicKey = _config["Vapid:PublicKey"];

        if (string.IsNullOrEmpty(publicKey))
        {
            return StatusCode(500, new { success = false, message = "VAPID public key not configured." });
        }

        return Ok(new { publicKey });
    }

    [HttpPost("/notifications/subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionDto subscription)
    {
        if (string.IsNullOrWhiteSpace(subscription.Endpoint))
            return BadRequest(new { success = false, message = "Invalid subscription data." });

        await _subscriptionService.AddSubscriptionAsync(subscription);
        return Ok(new { success = true, message = "Subscription saved." });
    }

    [HttpPost("/notifications/unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            return BadRequest(new { success = false, message = "Invalid endpoint." });

        await _subscriptionService.DeleteSubscriptionByEndpointAsync(endpoint);
        return Ok(new { success = true, message = "Subscription removed." });
    }
}
