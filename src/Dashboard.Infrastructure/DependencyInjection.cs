using Azure.Data.Tables;
using Dashboard.Application.Interfaces;
using Dashboard.Domain.Utils;
using Dashboard.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Dashboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddMemoryCache();

#if DEBUG
        // Debug mode: use dummy data service (scoped for consistency with Release mode)
        services.AddScoped<ITransactionService, DummyTransactionService>();
#else
        // Release mode: use Azure Table Storage
        services.AddSingleton<TableClient>(sp =>
        {
            var connectionString = Environment.GetEnvironmentVariable("TRANSACTIONS_TABLE_CONNECTION_STRING")!;
            var tableClient = new TableServiceClient(connectionString).GetTableClient(StaticDetails.TableName);
            tableClient.CreateIfNotExists();
            return tableClient;
        });

        services.AddScoped<ITransactionService, AzureTableService>();
#endif

        services.AddScoped<ITickerApiService, TickerApiService>();

        return services;
    }
}