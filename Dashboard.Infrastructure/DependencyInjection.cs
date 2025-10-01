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
            var cs = cfg["Secrets:TransactionsTableConnectionString"]!;
            var tableClient = new TableServiceClient(cs).GetTableClient(StaticDetails.TableName);
            tableClient.CreateIfNotExists();
            return tableClient;
        });

        services.AddScoped<IAzureTableService, AzureTableService>();
        services.AddScoped<ITickerApiService, TickerApiService>();

        return services;
    }
}