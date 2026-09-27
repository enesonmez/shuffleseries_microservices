using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ShuffleSeries.Shared.Core.Web.Health;

/// <summary>
/// Extension methods for registering and exposing standardized ASP.NET Core Health Checks.
/// Provides /health/live (Liveness) and /health/ready (Readiness) probes.
/// </summary>
public static class HealthCheckExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false
    };

    /// <summary>
    /// Registers core health check services into DI container.
    /// </summary>
    public static IHealthChecksBuilder AddSharedHealthChecks(this IServiceCollection services) =>
        services.AddHealthChecks();

    /// <summary>
    /// Maps /health/live and /health/ready endpoints with standardized JSON responses.
    /// </summary>
    public static IEndpointRouteBuilder MapSharedHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        // Liveness probe: returns 200 OK as long as the process is alive and responsive
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteLivenessResponse
        });

        // Readiness probe: verifies external dependencies (DB, Redis, Vault, Queue)
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            ResponseWriter = WriteReadinessResponse
        });

        return endpoints;
    }

    private static async Task WriteLivenessResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new
        {
            status = report.Status.ToString(),
            type = "Liveness",
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions));
    }

    private static async Task WriteReadinessResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new
        {
            status = report.Status.ToString(),
            type = "Readiness",
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                durationMs = entry.Value.Duration.TotalMilliseconds,
                description = entry.Value.Description,
                data = entry.Value.Data.Count != 0 ? entry.Value.Data : null,
                error = entry.Value.Exception?.Message
            })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions));
    }
}
