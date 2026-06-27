using Microsoft.Extensions.DependencyInjection;

namespace ShuffleSeries.Catalog.Application;

public static class DependencyInjection
{
    public static void AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });
    }
}