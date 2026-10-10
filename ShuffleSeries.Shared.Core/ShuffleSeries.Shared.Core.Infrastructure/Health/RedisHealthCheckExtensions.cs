using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ShuffleSeries.Shared.Core.Infrastructure.Health;

/// <summary>
/// Redis hazır bulunuşluk (Readiness) sağlık kontrolü için genişletme metotları.
/// </summary>
public static class RedisHealthCheckExtensions
{
    /// <summary>
    /// DI konteynerindeki IConnectionMultiplexer nesnesini kullanarak Redis sağlık kontrolünü boru hattına ekler.
    /// </summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <param name="name">Sağlık kontrolü adı (varsayılan: Redis).</param>
    /// <param name="failureStatus">Bağlantı hatası durumunda dönecek statü (varsayılan: Degraded).</param>
    /// <param name="customTags">İsteğe bağlı özel etiketler (varsayılan: ["cache", "redis", "ready"]).</param>
    public static IServiceCollection AddSharedRedisHealthCheck(
        this IServiceCollection services,
        string name = "Redis",
        HealthStatus failureStatus = HealthStatus.Degraded,
        IEnumerable<string>? customTags = null)
    {
        services.AddHealthChecks()
            .AddCheck<RedisHealthCheck>(
                name: name,
                failureStatus: failureStatus,
                tags: customTags ?? ["cache", "redis", "ready"]);

        return services;
    }
}
