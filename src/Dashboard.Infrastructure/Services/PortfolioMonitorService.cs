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
    private readonly Dictionary<string, decimal> _dailyOpenPrices = new();
    private DateTime _lastResetDate = DateTime.MinValue;

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
            try
            {
                await CheckAndSendScheduledUpdateAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during portfolio monitoring cycle");
            }

            await Task.Delay(TimeSpan.FromMinutes(StaticDetails.PortfolioCheckIntervalMinutes), stoppingToken);
        }
    }

    private async Task CheckAndSendScheduledUpdateAsync(CancellationToken stoppingToken)
    {
        var now = DateTime.Now;

        // Check if we're within notification hours
        if (now.Hour < StaticDetails.NotificationStartHour || now.Hour >= StaticDetails.NotificationEndHour)
        {
            _logger.LogInformation("Outside notification hours ({Start}:00 - {End}:00), skipping", 
                StaticDetails.NotificationStartHour, StaticDetails.NotificationEndHour);
            return;
        }

        // Reset daily prices if it's a new day
        if (_lastResetDate.Date != now.Date)
        {
            _logger.LogInformation("New day detected, resetting daily opening prices");
            _dailyOpenPrices.Clear();
            _lastResetDate = now;
        }

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

        // Store opening prices for tickers we haven't seen today
        foreach (var holding in topHoldings)
        {
            if (!_dailyOpenPrices.ContainsKey(holding.Ticker))
            {
                _dailyOpenPrices[holding.Ticker] = holding.CurrentPrice;
                _logger.LogInformation("Set daily opening price for {Ticker}: {Price}", 
                    holding.Ticker, holding.CurrentPrice);
            }
        }

        // Build notification message
        var title = "Portfolio update";
        var bodyLines = new List<string>();

        foreach (var holding in topHoldings)
        {
            var openingPrice = _dailyOpenPrices.GetValueOrDefault(holding.Ticker, holding.CurrentPrice);
            var changePercent = openingPrice != 0m
                ? (holding.CurrentPrice - openingPrice) / openingPrice * 100m
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
