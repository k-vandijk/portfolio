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

        services.AddSingleton<TableClient>(sp =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var connectionString = Environment.GetEnvironmentVariable("TRANSACTIONS_TABLE_CONNECTION_STRING")!;
            var tableClient = new TableServiceClient(connectionString).GetTableClient(StaticDetails.TableName);
            tableClient.CreateIfNotExists();
            return tableClient;
        });

        services.AddScoped<IAzureTableService, AzureTableService>();
        services.AddScoped<ITickerApiService, TickerApiService>();

        return services;
    }
}