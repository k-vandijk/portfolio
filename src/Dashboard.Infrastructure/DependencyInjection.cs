using Azure.Data.Tables;
using Dashboard.Application.Interfaces;
using Dashboard.Domain.Utils;
using Dashboard.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dashboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddKeyedSingleton<TableClient>(StaticDetails.TransactionsTableName, (sp, _) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("StorageAccount");
            var tableClient = new TableServiceClient(connectionString).GetTableClient(StaticDetails.TransactionsTableName);
            tableClient.CreateIfNotExists();
            return tableClient;
        });

        services.AddKeyedSingleton<TableClient>(StaticDetails.PushSubscriptionsTableName, (sp, _) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("StorageAccount");
            var tableClient = new TableServiceClient(connectionString).GetTableClient(StaticDetails.PushSubscriptionsTableName);
            tableClient.CreateIfNotExists();
            return tableClient;
        });

        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ITickerApiService, TickerApiService>();
        services.AddScoped<IPushSubscriptionService, PushSubscriptionService>();
        services.AddScoped<IPushNotificationService, PushNotificationService>();
        services.AddScoped<IPortfolioValueService, PortfolioValueService>();

        services.AddHostedService<PortfolioMonitorService>();

        return services;
    }
}
