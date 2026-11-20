using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Identity.Application.Common.Interfaces;
using ShuffleSeries.Identity.Application.Common.Services;
using ShuffleSeries.Shared.Core.Application;
using ShuffleSeries.Shared.Core.Application.Middlewares;
using Wolverine;
using Wolverine.FluentValidation;

namespace ShuffleSeries.Identity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddSharedApplication();
        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<IAuthResponseService, AuthResponseService>();

        return services;
    }

    public static void AddIdentityApplicationMessaging(this WolverineOptions opts)
    {
        opts.Discovery.IncludeAssembly(typeof(DependencyInjection).Assembly);
        opts.RestoreV5Defaults();
        opts.UseFluentValidation();
        opts.Policies.AddMiddleware(typeof(PerformanceMiddleware));
        opts.Policies.AddMiddleware(typeof(LoggingMiddleware));
    }
}
