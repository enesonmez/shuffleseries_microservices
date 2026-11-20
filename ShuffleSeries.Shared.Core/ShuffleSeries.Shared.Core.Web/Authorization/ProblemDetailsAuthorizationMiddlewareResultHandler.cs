using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using ShuffleSeries.Shared.Core.Web.Helpers;

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
            await ProblemDetailsWriter.WriteAsync(
                context,
                new ProblemDetailsDescriptor(
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    "Bu kaynağa erişmek için gerekli yetkiye (rol veya izin) sahip değilsiniz.",
                    "AUTH_FORBIDDEN",
                    "https://tools.ietf.org/html/rfc7231#section-6.5.3"));
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
