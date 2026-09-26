using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Shared.Core.Infrastructure.Interceptors;

namespace ShuffleSeries.Shared.Core.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<InsertOutboxMessagesInterceptor>();
        services.AddSingleton<SoftDeleteInterceptor>();
        return services;
    }
}
