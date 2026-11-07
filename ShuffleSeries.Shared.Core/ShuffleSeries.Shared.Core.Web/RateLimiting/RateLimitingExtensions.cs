using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Shared.Core.Web.Extensions;
using ShuffleSeries.Shared.Core.Web.Helpers;

namespace ShuffleSeries.Shared.Core.Web.RateLimiting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddSharedRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                await ProblemDetailsWriter.WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status429TooManyRequests,
                    "Too Many Requests",
                    "Rate limit exceeded. Please try again later.",
                    "RATE_LIMIT_EXCEEDED",
                    "https://tools.ietf.org/html/rfc6585#section-4",
                    cancellationToken: cancellationToken);
            };

            // Global Limiter: Sistemin genelini kaba kuvvet (brute-force) ve DDoS saldırılarına karşı korur.
            // Kullanıcı login ise kullanıcı bazlı, değilse IP başına saniyede 100 isteğe izin verir.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var userId = context.User.TryGetUserId();
                var partitionKey = userId.HasValue
                    ? $"user:{userId.Value}"
                    : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: partitionKey,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100, // Saniyede 100 istek
                        QueueLimit = 0,    // Kuyruklama yapma, anında reject et
                        Window = TimeSpan.FromSeconds(1)
                    });
            });

            // Strict Limiter: Özellikle auth/login ve shuffle/swipe gibi hassas/ağır işlemler için katı sınır.
            // Client IP başına saniyede 5 isteğe izin verir (Guest token minting ile bypass edilmesini önlemek için doğrudan IP ile bölümlenir).
            options.AddPolicy("StrictLimiter", context =>
            {
                var partitionKey = $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: partitionKey,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 5,
                        QueueLimit = 0,
                        Window = TimeSpan.FromSeconds(1)
                    });
            });
        });

        return services;
    }
}
