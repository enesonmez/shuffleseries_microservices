using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Shared.Core.Application;
using ShuffleSeries.Shared.Core.Application.Middlewares;
using Wolverine;
using Wolverine.FluentValidation;

namespace ShuffleSeries.Catalog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddSharedApplication();
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }

    public static void AddCatalogApplicationMessaging(this WolverineOptions opts)
    {
        opts.Discovery.IncludeAssembly(typeof(DependencyInjection).Assembly);
        opts.RestoreV5Defaults();
        opts.UseFluentValidation();
        opts.Policies.AddMiddleware(typeof(PerformanceMiddleware));
        opts.Policies.AddMiddleware(typeof(LoggingMiddleware));
    }
}
