using Dashboard.Application.Interfaces;
using Dashboard.Application.Middleware;
using Dashboard.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Dashboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {

        services.AddHttpClient("cached-http-client")
            .AddHttpMessageHandler(sp =>
                new HttpGetCachingHandler(
                    sp.GetRequiredService<IMemoryCache>(),
                    absoluteTtl: TimeSpan.FromMinutes(10),
                    slidingTtl: TimeSpan.FromMinutes(5)
                )
            );

        services.AddScoped<IAzureTableService, AzureTableService>();
        services.AddScoped<ITickerApiService, TickerApiService>();

        return services;
    }
}