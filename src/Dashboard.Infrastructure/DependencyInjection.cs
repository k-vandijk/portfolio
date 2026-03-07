using Azure.Data.Tables;
using Dashboard.Application.HttpClientInterfaces;
using Dashboard.Application.RepositoryInterfaces;
using Dashboard.Application.ServiceInterfaces;
using Dashboard.Infrastructure.HttpClients;
using Dashboard.Infrastructure.Repositories;
using Dashboard.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dashboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddSingleton<TableServiceClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("StorageAccount");
            return new TableServiceClient(connectionString);
        });

        services.AddScoped<IPortfolioAnalysesRepository, PortfolioAnalysesRepository>();
        services.AddScoped<IPushSubscriptionsRepository, PushSubscriptionsRepository>();
        services.AddScoped<ITransactionsRepository, TransactionsRepository>();
        services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();

        services.AddScoped<ITickerApiClient, TickerApiClient>();

        services.AddScoped<IPushNotificationService, PushNotificationService>();
        services.AddScoped<IPortfolioValueService, PortfolioValueService>();
        services.AddScoped<IPortfolioAnalysisService, PortfolioAnalysisService>();

        services.AddHostedService<PortfolioMonitorBackgroundService>();
        services.AddHostedService<WeeklyAnalysisBackgroundService>();

        return services;
    }
}
