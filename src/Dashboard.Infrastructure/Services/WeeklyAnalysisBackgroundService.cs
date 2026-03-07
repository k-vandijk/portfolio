using Dashboard.Application.ServiceInterfaces;
using Dashboard.Domain.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebPush;

namespace Dashboard.Infrastructure.Services;

/// <summary>
/// Background service that runs a weekly AI portfolio analysis.
/// It checks every hour whether it is time to run; analysis is triggered once
/// per week during the <see cref="StaticDetails.WeeklyAnalysisRunHour"/> hour window.
/// </summary>
public class WeeklyAnalysisBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WeeklyAnalysisBackgroundService> _logger;

    public WeeklyAnalysisBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<WeeklyAnalysisBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let the app finish starting up before doing any work
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TryRunAnalysisIfDueAsync();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Unexpected error in WeeklyAnalysisBackgroundService");
            }

            // Check again in one hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task TryRunAnalysisIfDueAsync()
    {
        var now = DateTime.Now;

        // Only attempt analysis during the designated hour window
        if (now.Hour != StaticDetails.WeeklyAnalysisRunHour)
            return;

        using var scope = _scopeFactory.CreateScope();
        var analysisService = scope.ServiceProvider.GetRequiredService<IPortfolioAnalysisService>();

        // Fetch the most recent weekly analysis to see if 7 days have passed
        var recent = await analysisService.GetRecentAnalysesAsync(1);
        var lastAnalysis = recent.FirstOrDefault(a => a.AnalysisType == "weekly");

        if (lastAnalysis is not null)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var daysSinceLast = today.DayNumber - lastAnalysis.AnalysisDate.DayNumber;
            if (daysSinceLast < StaticDetails.WeeklyAnalysisIntervalDays)
            {
                _logger.LogDebug(
                    "Weekly analysis skipped — last ran {Days} day(s) ago (minimum interval: {Min})",
                    daysSinceLast,
                    StaticDetails.WeeklyAnalysisIntervalDays);
                return;
            }
        }

        _logger.LogInformation("Running weekly portfolio analysis");
        await analysisService.RunWeeklyAnalysisAsync();

        await SendAnalysisNotificationAsync(scope);
    }

    private async Task SendAnalysisNotificationAsync(IServiceScope scope)
    {
        var subscriptionService = scope.ServiceProvider.GetRequiredService<IPushSubscriptionService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();
        var analysisService = scope.ServiceProvider.GetRequiredService<IPortfolioAnalysisService>();

        var subscriptions = await subscriptionService.GetSubscriptionsAsync();
        if (subscriptions.Count == 0) return;

        var recent = await analysisService.GetRecentAnalysesAsync(1);
        var weekNumber = recent.FirstOrDefault(a => a.AnalysisType == "weekly")?.WeekNumber ?? 0;

        var month = DateTime.Today.ToString("MMMM");
        var title = StaticDetails.AnalysisNotificationGreetings[
            Random.Shared.Next(StaticDetails.AnalysisNotificationGreetings.Length)];
        var body = $"Week {weekNumber} of {month} is ready\n━━━━━━━━━━━━━━━━━━\nYour portfolio has been reviewed and analysed.";

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
                _logger.LogWarning(ex, "Failed to send analysis notification to {Endpoint}", sub.Endpoint);
            }
        }
    }
}
