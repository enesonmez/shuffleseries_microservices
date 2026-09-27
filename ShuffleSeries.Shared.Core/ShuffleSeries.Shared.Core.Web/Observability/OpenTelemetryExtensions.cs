using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ShuffleSeries.Shared.Core.Web.Observability;

/// <summary>
/// Extension methods for configuring OpenTelemetry distributed tracing and metrics.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Configures OpenTelemetry with standard ASP.NET Core and HttpClient instrumentation,
    /// and optionally registers an OTLP exporter if an endpoint is configured.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="serviceName">Logical service name (e.g. Catalog.Api).</param>
    /// <param name="serviceVersion">Optional service version.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSharedOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        string? serviceVersion = null)
    {
        var options = configuration.GetSection(OpenTelemetryOptions.SectionName).Get<OpenTelemetryOptions>()
                      ?? new OpenTelemetryOptions();

        if (!options.Enabled)
        {
            return services;
        }

        var otlpEndpoint = options.OtlpEndpoint
                           ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceVersion: serviceVersion ?? "1.0.0")
                .AddEnvironmentVariableDetector())
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(httpOptions =>
                    {
                        httpOptions.RecordException = true;
                        // Exclude health check endpoints from trace noise
                        httpOptions.Filter = httpContext =>
                            !httpContext.Request.Path.StartsWithSegments("/health");
                    })
                    .AddHttpClientInstrumentation(clientOptions =>
                    {
                        clientOptions.RecordException = true;
                    });

                if (!string.IsNullOrWhiteSpace(otlpEndpoint) && Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpointUri))
                {
                    tracing.AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = endpointUri;
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint) && Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpointUri))
                {
                    metrics.AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = endpointUri;
                    });
                }
            });

        return services;
    }
}
