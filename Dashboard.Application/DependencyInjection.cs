using Microsoft.Extensions.DependencyInjection;

namespace Dashboard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}