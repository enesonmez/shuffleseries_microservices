using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Shared.Core.Application.Behaviors;
using FluentValidation;

namespace ShuffleSeries.Catalog.Application;

public static class DependencyInjection
{
    public static void AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;
        
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);

            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        
        services.AddValidatorsFromAssembly(assembly);
    }
}