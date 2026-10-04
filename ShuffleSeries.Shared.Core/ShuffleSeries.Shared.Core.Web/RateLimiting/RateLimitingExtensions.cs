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
            {
                // TraceIdentifier her istekte benzersiz olduğu için fallback olarak kullanılırsa limiter işe yaramaz.
                // Bu yüzden IP bulunamazsa (örn. test ortamı veya eksik header), "anonymous" gibi statik bir key atanmalıdır.
                // Not: Gateway eğer AWS ALB/Cloudflare arkasındaysa UseForwardedHeaders middleware'i kesinlikle yapılandırılmalıdır.
                var partitionKey = context.User.Identity?.IsAuthenticated == true
                    ? context.User.Identity.Name! // Oturum açmışsa kullanıcıya özel
                    : context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

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
            // IP başına saniyede 5 isteğe izin verir.
            options.AddPolicy("StrictLimiter", context =>
            {
                var partitionKey = context.User.Identity?.IsAuthenticated == true
                    ? context.User.Identity.Name!
                    : context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

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
