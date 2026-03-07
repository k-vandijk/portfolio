using Dashboard.Application.Dtos;
using Dashboard.Application.Mappers;
using Dashboard.Application.RepositoryInterfaces;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard._Web.Controllers;

public class NotificationsController : Controller
{
    private readonly IPushSubscriptionsRepository _subscriptionsRepository;
    private readonly IConfiguration _config;

    public NotificationsController(
        IPushSubscriptionsRepository subscriptionsRepository,
        IConfiguration config)
    {
        _subscriptionsRepository = subscriptionsRepository;
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

        var all = await _subscriptionsRepository.GetAllAsync();
        if (all.Any(e => e.Endpoint == subscription.Endpoint))
            return Ok(new { success = true, message = "Subscription saved." });

        await _subscriptionsRepository.AddAsync(subscription.ToEntity());
        return Ok(new { success = true, message = "Subscription saved." });
    }

    [HttpPost("/notifications/unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            return BadRequest(new { success = false, message = "Invalid endpoint." });

        var all = await _subscriptionsRepository.GetAllAsync();
        foreach (var entity in all.Where(e => e.Endpoint == endpoint))
            await _subscriptionsRepository.DeleteAsync(entity);

        return Ok(new { success = true, message = "Subscription removed." });
    }
}
