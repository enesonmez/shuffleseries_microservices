using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ShuffleSeries.Shared.Core.Web.RateLimiting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddSharedRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // İstemciye 429 Too Many Requests dönülecek
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Global Limiter: Sistemin genelini kaba kuvvet (brute-force) ve DDoS saldırılarına karşı korur.
            // IP başına saniyede 100 isteğe izin verir.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? context.TraceIdentifier,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100, // Saniyede 100 istek
                        QueueLimit = 0,    // Kuyruklama yapma, anında reject et
                        Window = TimeSpan.FromSeconds(1)
                    }));

            // Strict Limiter: Özellikle auth/login ve shuffle/swipe gibi hassas/ağır işlemler için katı sınır.
            // IP başına saniyede 5 isteğe izin verir.
            options.AddPolicy("StrictLimiter", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? context.TraceIdentifier,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 5,
                        QueueLimit = 0,
                        Window = TimeSpan.FromSeconds(1)
                    }));
        });

        return services;
    }
}
