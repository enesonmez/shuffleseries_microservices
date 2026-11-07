using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Shared.Core.Web.Correlation;

namespace ShuffleSeries.Shared.Core.Web.Helpers;

public static class ProblemDetailsWriter
{
    public const string ProblemJsonContentType = "application/problem+json";

    public static async Task WriteAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        string code,
        string? type = null,
        Action<Microsoft.AspNetCore.Mvc.ProblemDetails>? configure = null,
        CancellationToken cancellationToken = default)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = ProblemJsonContentType;

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var correlationContext = context.RequestServices?.GetService<ICorrelationIdContext>();
        var correlationId = correlationContext?.CorrelationId;

        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type ?? $"https://httpstatuses.io/{statusCode}",
            Instance = context.Request.Path
        };

        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = traceId;
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            problem.Extensions["correlationId"] = correlationId;
        }

        configure?.Invoke(problem);

        await context.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: ProblemJsonContentType,
            cancellationToken: cancellationToken == default ? context.RequestAborted : cancellationToken);
    }
}
