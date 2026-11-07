namespace ShuffleSeries.ApiGateway.Api.Middlewares;

/// <summary>
/// TokenRevocationMiddleware kayıt genişletmeleri.
/// </summary>
public static class TokenRevocationMiddlewareExtensions
{
    /// <summary>
    /// API Gateway Edge katmanında iptal edilmiş token'ları yakalayarak durduran güvenlik middleware'ini pipeline'a ekler.
    /// </summary>
    public static IApplicationBuilder UseTokenRevocationGuard(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TokenRevocationMiddleware>();
    }
}
