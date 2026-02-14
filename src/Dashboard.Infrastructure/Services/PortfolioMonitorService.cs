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
    private decimal? _lastKnownWorth;

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
                await CheckPortfolioAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during portfolio monitoring cycle");
            }

            await Task.Delay(TimeSpan.FromMinutes(StaticDetails.PortfolioCheckIntervalMinutes), stoppingToken);
        }
    }

    private async Task CheckPortfolioAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var portfolioService = scope.ServiceProvider.GetRequiredService<IPortfolioValueService>();
        var subscriptionService = scope.ServiceProvider.GetRequiredService<IPushSubscriptionService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();

        var currentWorth = await portfolioService.GetCurrentPortfolioWorthAsync();

        if (_lastKnownWorth is null)
        {
            _lastKnownWorth = currentWorth;
            _logger.LogInformation("Baseline portfolio worth set to {Worth}", currentWorth);
            return;
        }

        var changePercent = _lastKnownWorth.Value != 0m
            ? (currentWorth - _lastKnownWorth.Value) / _lastKnownWorth.Value * 100m
            : 0m;

        _logger.LogInformation(
            "Portfolio check: Previous={Previous}, Current={Current}, Change={Change}%",
            _lastKnownWorth.Value, currentWorth, Math.Round(changePercent, 2));

        if (Math.Abs(changePercent) >= StaticDetails.PortfolioChangeThresholdPercent)
        {
            var direction = changePercent > 0 ? "+" : "";
            var title = "Portfolio Alert";
            var body = $"Your portfolio shifted {direction}{Math.Round(changePercent, 2)}% (now {currentWorth:C})";

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

        _lastKnownWorth = currentWorth;
    }
}
