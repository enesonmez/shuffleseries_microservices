using Microsoft.AspNetCore.Authentication;

namespace ShuffleSeries.ApiGateway.Api.Middlewares;

/// <summary>
/// API Gateway Edge katmanında iptal edilmiş (kara listede olan) JWT belirteçlerini yakalayarak
/// rotanın anonim veya yetkili olmasından bağımsız olarak alt mikroservislere geçişini engelleyen middleware.
/// </summary>
public sealed class TokenRevocationMiddleware
{
    private readonly RequestDelegate _next;

    public TokenRevocationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Items.ContainsKey("IsRevoked"))
        {
            await context.ChallengeAsync();
            return;
        }

        await _next(context);
    }
}
