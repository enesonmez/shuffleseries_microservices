using Microsoft.Extensions.DependencyInjection;

namespace ShuffleSeries.Shared.Core.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Extension method for configuring Shared.Core.Application services.
    /// </summary>
    public static IServiceCollection AddSharedApplication(this IServiceCollection services) => services;
}
