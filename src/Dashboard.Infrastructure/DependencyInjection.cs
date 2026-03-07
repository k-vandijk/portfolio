using Azure.Data.Tables;
using Dashboard.Application.RepositoryInterfaces;
using Dashboard.Application.ServiceInterfaces;
using Dashboard.Domain.Utils;
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

        services.AddKeyedSingleton<TableClient>(StaticDetails.PushSubscriptionsTableName, (sp, _) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("StorageAccount");
            var tableClient = new TableServiceClient(connectionString).GetTableClient(StaticDetails.PushSubscriptionsTableName);
            tableClient.CreateIfNotExists();
            return tableClient;
        });

        services.AddKeyedSingleton<TableClient>(StaticDetails.AiAnalysesTableName, (sp, _) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("StorageAccount");
            var tableClient = new TableServiceClient(connectionString).GetTableClient(StaticDetails.AiAnalysesTableName);
            tableClient.CreateIfNotExists();
            return tableClient;
        });

        services.AddScoped<ITickerApiService, TickerApiService>();
        services.AddScoped<IPushSubscriptionService, PushSubscriptionService>();
        services.AddScoped<IPushNotificationService, PushNotificationService>();
        services.AddScoped<IPortfolioValueService, PortfolioValueService>();
        services.AddScoped<IPortfolioAnalysisService, PortfolioAnalysisService>();

        services.AddHostedService<PortfolioMonitorBackgroundService>();
        services.AddHostedService<WeeklyAnalysisBackgroundService>();

        return services;
    }
}
