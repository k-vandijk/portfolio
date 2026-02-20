using Dashboard.Application.Interfaces;
using Dashboard.Domain.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebPush;

namespace Dashboard.Infrastructure.Services;

public class PortfolioMonitorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PortfolioMonitorService> _logger;

    public PortfolioMonitorService(IServiceScopeFactory scopeFactory, ILogger<PortfolioMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial delay to let the app start up
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextRun = GetNextScheduledTime(now);
            var delay = nextRun - now;

            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, stoppingToken);

            try
            {
                await CheckAndSendScheduledUpdateAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during portfolio monitoring cycle");
            }
        }
    }

    internal static DateTime GetNextScheduledTime(DateTime now)
    {
        var interval = StaticDetails.PortfolioCheckIntervalMinutes / 60;
        var startHour = StaticDetails.NotificationStartHour;
        var endHour = StaticDetails.NotificationEndHour;

        // Find the next slot today
        for (var hour = startHour; hour <= endHour; hour += interval)
        {
            var candidate = now.Date.AddHours(hour);
            if (candidate > now)
                return candidate;
        }

        // All today's slots have passed — schedule for first slot tomorrow
        return now.Date.AddDays(1).AddHours(startHour);
    }

    private async Task CheckAndSendScheduledUpdateAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var portfolioService = scope.ServiceProvider.GetRequiredService<IPortfolioValueService>();
        var subscriptionService = scope.ServiceProvider.GetRequiredService<IPushSubscriptionService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();

        // Get top 3 holdings by value
        var topHoldings = await portfolioService.GetTopHoldingsByValueAsync(3);

        if (topHoldings.Count == 0)
        {
            _logger.LogInformation("No holdings found, skipping notification");
            return;
        }

        // Build notification message
        var title = "Portfolio update";
        var bodyLines = new List<string>();

        foreach (var holding in topHoldings)
        {
            var changePercent = holding.PreviousDayClose != 0m
                ? (holding.CurrentPrice - holding.PreviousDayClose) / holding.PreviousDayClose * 100m
                : 0m;

            var direction = changePercent >= 0 ? "+" : "";
            var line = $"{holding.Ticker}: {direction}{Math.Round(changePercent, 1)}% ({holding.TotalValue:C})";
            bodyLines.Add(line);
        }

        var body = string.Join("\n", bodyLines);

        _logger.LogInformation("Sending scheduled portfolio update notification");

        // Send notification to all subscribers
        var subscriptions = await subscriptionService.GetSubscriptionsAsync();

        foreach (var sub in subscriptions)
        {
            try
            {
                await notificationService.SendNotificationAsync(sub, title, body);
            }
            catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                _logger.LogInformation("Removing expired subscription: {Endpoint}", sub.Endpoint);
                await subscriptionService.DeleteSubscriptionByEndpointAsync(sub.Endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send notification to {Endpoint}", sub.Endpoint);
            }
        }
    }
}
