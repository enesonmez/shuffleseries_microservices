using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace ShuffleSeries.Shared.Core.Web.Correlation;

/// <summary>
/// Middleware that inspects incoming HTTP requests for an X-Correlation-ID header,
/// creates a new correlation ID if missing, echoes it on the response header,
/// and enriches Serilog's LogContext and OpenTelemetry's Activity tag.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    private const int MaxCorrelationIdLength = 128;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationIdContext correlationContext)
    {
        var rawCorrelationId = context.Request.Headers[CorrelationIdConstants.HeaderName].FirstOrDefault();

        var correlationId = IsValidCorrelationId(rawCorrelationId)
            ? rawCorrelationId!
            : Guid.NewGuid().ToString("N");

        correlationContext.SetCorrelationId(correlationId);

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdConstants.HeaderName))
            {
                context.Response.Headers.Append(CorrelationIdConstants.HeaderName, correlationId);
            }

            return Task.CompletedTask;
        });

        Activity.Current?.SetTag(CorrelationIdConstants.ActivityTagName, correlationId);

        using (LogContext.PushProperty(CorrelationIdConstants.LogPropertyName, correlationId))
        {
            await _next(context);
        }
    }

    private static bool IsValidCorrelationId(string? correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > MaxCorrelationIdLength)
        {
            return false;
        }

        // Reject control characters (such as CRLF) to prevent header and log injection attacks
        return !correlationId.Any(char.IsControl);
    }
}
