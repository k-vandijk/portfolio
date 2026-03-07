using Azure.Data.Tables;
using Kvandijk.Portfolio.Application.HttpClientInterfaces;
using Kvandijk.Portfolio.Application.RepositoryInterfaces;
using Kvandijk.Portfolio.Application.ServiceInterfaces;
using Kvandijk.Portfolio.Infrastructure.HttpClients;
using Kvandijk.Portfolio.Infrastructure.Repositories;
using Kvandijk.Portfolio.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kvandijk.Portfolio.Infrastructure;

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
