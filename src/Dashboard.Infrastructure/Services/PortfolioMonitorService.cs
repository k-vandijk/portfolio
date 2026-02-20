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
                await CheckAndSendScheduledUpdateAsync();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during portfolio monitoring cycle");
            }
        }
    }
    
    private static string PickRandomGreeting() =>
        StaticDetails.NotificationGreetings[Random.Shared.Next(StaticDetails.NotificationGreetings.Length)];

    private static DateTime GetNextScheduledTime(DateTime now)
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

    private async Task CheckAndSendScheduledUpdateAsync()
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
        var title = PickRandomGreeting();
        var bodyLines = new List<string>();

        var totalPrevValue = 0m;
        var totalCurrentValue = 0m;

        foreach (var holding in topHoldings)
        {
            var prevValue = holding.PreviousDayClose * holding.Quantity;
            var currentValue = holding.TotalValue;
            totalPrevValue += prevValue;
            totalCurrentValue += currentValue;

            var changePercent = holding.PreviousDayClose != 0m
                ? (holding.CurrentPrice - holding.PreviousDayClose) / holding.PreviousDayClose * 100m
                : 0m;
            var absChange = (holding.CurrentPrice - holding.PreviousDayClose) * holding.Quantity;

            var sign = absChange >= 0 ? "+" : "";
            bodyLines.Add($"{holding.Ticker}: {sign}{Math.Round(changePercent, 1)}% ({sign}{absChange:C})");
        }

        var totalAbsChange = totalCurrentValue - totalPrevValue;
        var totalChangePercent = totalPrevValue != 0m
            ? (totalCurrentValue - totalPrevValue) / totalPrevValue * 100m
            : 0m;
        var totalSign = totalAbsChange >= 0 ? "+" : "";

        bodyLines.Add($"━━━━━━━━━━━━━━━━━━");
        bodyLines.Add($"Total: {totalSign}{Math.Round(totalChangePercent, 1)}% ({totalSign}{totalAbsChange:C})");

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
