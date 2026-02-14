using Dashboard.Application.Dtos;
using Dashboard.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard._Web.Controllers;

public class NotificationsController : Controller
{
    private readonly IPushSubscriptionService _subscriptionService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(IPushSubscriptionService subscriptionService, ILogger<NotificationsController> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    [HttpGet("/notifications/vapid-public-key")]
    public IActionResult GetVapidPublicKey()
    {
        var publicKey = Environment.GetEnvironmentVariable("VAPID_PUBLIC_KEY");

        if (string.IsNullOrEmpty(publicKey))
        {
            return StatusCode(500, new { success = false, message = "VAPID public key not configured." });
        }

        return Ok(new { publicKey });
    }

    [HttpPost("/notifications/subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionDto subscription)
    {
        if (subscription == null || string.IsNullOrWhiteSpace(subscription.Endpoint))
        {
            return BadRequest(new { success = false, message = "Invalid subscription data." });
        }

        try
        {
            await _subscriptionService.AddSubscriptionAsync(subscription);
            return Ok(new { success = true, message = "Subscription saved." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving push subscription");
            return StatusCode(500, new { success = false, message = "Error saving subscription.", detail = ex.Message });
        }
    }

    [HttpPost("/notifications/unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return BadRequest(new { success = false, message = "Invalid endpoint." });
        }

        try
        {
            await _subscriptionService.DeleteSubscriptionByEndpointAsync(endpoint);
            return Ok(new { success = true, message = "Subscription removed." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing push subscription");
            return StatusCode(500, new { success = false, message = "Error removing subscription.", detail = ex.Message });
        }
    }
}
