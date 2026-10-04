using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Shared.Core.Web.Correlation;

namespace ShuffleSeries.Shared.Core.Web.Authorization;

public sealed class ProblemDetailsAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.StatusCode = StatusCodes.Status403Forbidden;

            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            var correlationContext = context.RequestServices.GetService<ICorrelationIdContext>();
            var correlationId = correlationContext?.CorrelationId;

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                Title = "Forbidden",
                Detail = "Bu kaynağa erişmek için gerekli yetkiye (rol veya izin) sahip değilsiniz.",
                Instance = context.Request.Path
            };

            problemDetails.Extensions["code"] = "AUTH_FORBIDDEN";
            problemDetails.Extensions["traceId"] = traceId;
            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                problemDetails.Extensions["correlationId"] = correlationId;
            }

            await context.Response.WriteAsJsonAsync(
                problemDetails,
                options: null,
                contentType: "application/problem+json",
                cancellationToken: context.RequestAborted);

            return;
        }

        // Başarılı (Succeeded) veya Challenged (401) durumlarında varsayılan framework işleyişini sürdür.
        // Challenged durumu JwtBearerEvents.OnChallenge tarafından yakalanarak RFC 7807 formatında dönecektir.
        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
