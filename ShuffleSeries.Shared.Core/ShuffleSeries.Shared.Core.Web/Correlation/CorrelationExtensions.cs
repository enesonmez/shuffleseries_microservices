using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ShuffleSeries.Shared.Core.Web.Correlation;

/// <summary>
/// Extension methods for configuring distributed Correlation ID tracking.
/// </summary>
public static class CorrelationExtensions
{
    /// <summary>
    /// Registers correlation ID context and HTTP client delegating handler.
    /// </summary>
    public static IServiceCollection AddSharedCorrelation(this IServiceCollection services)
    {
        services.AddScoped<ICorrelationIdContext, CorrelationIdContext>();
        services.AddTransient<CorrelationIdDelegatingHandler>();
        return services;
    }

    /// <summary>
    /// Adds CorrelationIdMiddleware into the ASP.NET Core request pipeline.
    /// </summary>
    public static IApplicationBuilder UseSharedCorrelation(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();
}
