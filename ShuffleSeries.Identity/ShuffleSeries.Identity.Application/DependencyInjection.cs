using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Identity.Application.Common.Interfaces;
using ShuffleSeries.Identity.Application.Common.Services;
using ShuffleSeries.Shared.Core.Application;
using ShuffleSeries.Shared.Core.Application.Behaviors;

namespace ShuffleSeries.Identity.Application;

public static class DependencyInjection
{
    public static void AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddSharedApplication();

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);

            config.AddOpenBehavior(typeof(LoggingBehavior<,>));
            config.AddOpenBehavior(typeof(PerformanceBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<IAuthResponseService, AuthResponseService>();
    }
}
