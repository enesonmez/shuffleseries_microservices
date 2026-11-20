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
        ProblemDetailsDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(descriptor);

        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = descriptor.StatusCode;
        context.Response.ContentType = ProblemJsonContentType;

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var correlationContext = context.RequestServices?.GetService<ICorrelationIdContext>();
        var correlationId = correlationContext?.CorrelationId;

        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = descriptor.StatusCode,
            Title = descriptor.Title,
            Detail = descriptor.Detail,
            Type = descriptor.Type ?? $"https://httpstatuses.io/{descriptor.StatusCode}",
            Instance = context.Request.Path
        };

        problem.Extensions["code"] = descriptor.Code;
        problem.Extensions["traceId"] = traceId;
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            problem.Extensions["correlationId"] = correlationId;
        }

        descriptor.Configure?.Invoke(problem);

        await context.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: ProblemJsonContentType,
            cancellationToken: cancellationToken == default ? context.RequestAborted : cancellationToken);
    }
}
