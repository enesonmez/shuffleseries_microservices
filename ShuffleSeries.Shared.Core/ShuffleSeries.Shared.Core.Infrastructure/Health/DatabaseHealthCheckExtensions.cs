using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ShuffleSeries.Shared.Core.Infrastructure.Health;

/// <summary>
/// EF Core DbContext veritabanı bağlantılarını ASP.NET Core Health Checks boru hattına
/// standart etiketler ve hata politikaları ile kaydeden genişletme metotları.
/// </summary>
public static class DatabaseHealthCheckExtensions
{
    /// <summary>
    /// Belirtilen DbContext için veritabanı hazır bulunuşluk (Readiness) sağlık kontrolünü DI konteynerine ekler.
    /// </summary>
    /// <typeparam name="TDbContext">Sağlık kontrolü yapılacak EF Core DbContext tipi.</typeparam>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <param name="name">Sağlık kontrolü adı (varsayılan: PostgreSQL).</param>
    /// <param name="failureStatus">Bağlantı hatası durumunda dönecek statü (varsayılan: Unhealthy).</param>
    /// <param name="customTags">İsteğe bağlı özel etiketler (varsayılan: ["db", "postgresql", "ready"]).</param>
    /// <returns>Servis koleksiyonu zinciri.</returns>
    public static IServiceCollection AddSharedDatabaseHealthCheck<TDbContext>(
        this IServiceCollection services,
        string name = "PostgreSQL",
        HealthStatus failureStatus = HealthStatus.Unhealthy,
        IEnumerable<string>? customTags = null)
        where TDbContext : DbContext
    {
        services.AddHealthChecks()
            .AddDbContextCheck<TDbContext>(
                name: name,
                failureStatus: failureStatus,
                tags: customTags ?? ["db", "postgresql", "ready"]);

        return services;
    }
}
