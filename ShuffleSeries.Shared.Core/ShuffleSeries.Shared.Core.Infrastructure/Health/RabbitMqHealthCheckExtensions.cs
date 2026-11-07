using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ShuffleSeries.Shared.Core.Infrastructure.Health;

/// <summary>
/// RabbitMQ hazır bulunuşluk (Readiness) sağlık kontrolü için genişletme metotları.
/// </summary>
public static class RabbitMqHealthCheckExtensions
{
    /// <summary>
    /// DI konteynerindeki RabbitMQ bağlantı yapılandırmasını kullanarak RabbitMQ sağlık kontrolünü boru hattına ekler.
    /// </summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <param name="configuration">İsteğe bağlı konfigürasyon nesnesi (belirtilmezse DI'dan çözülür).</param>
    /// <param name="name">Sağlık kontrolü adı (varsayılan: RabbitMQ).</param>
    /// <param name="failureStatus">Bağlantı hatası durumunda dönecek statü (varsayılan: Degraded).</param>
    /// <param name="customTags">İsteğe bağlı özel etiketler (varsayılan: ["messaging", "rabbitmq", "ready"]).</param>
    public static IServiceCollection AddSharedRabbitMqHealthCheck(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string name = "RabbitMQ",
        HealthStatus failureStatus = HealthStatus.Degraded,
        IEnumerable<string>? customTags = null)
    {
        if (configuration != null)
        {
            services.AddHealthChecks()
                .AddTypeActivatedCheck<RabbitMqHealthCheck>(
                    name: name,
                    failureStatus: failureStatus,
                    tags: customTags ?? ["messaging", "rabbitmq", "ready"],
                    args: [configuration]);
        }
        else
        {
            services.AddHealthChecks()
                .AddCheck<RabbitMqHealthCheck>(
                    name: name,
                    failureStatus: failureStatus,
                    tags: customTags ?? ["messaging", "rabbitmq", "ready"]);
        }

        return services;
    }
}
