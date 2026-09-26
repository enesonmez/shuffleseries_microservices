using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ShuffleSeries.Shared.Core.Web.Cors;

/// <summary>
/// Tüm mikroservisler ve API Gateway için merkezi CORS (Cross-Origin Resource Sharing) yapılandırma extension'ları.
/// </summary>
public static class CorsExtensions
{
    public const string DefaultCorsPolicyName = "DefaultCorsPolicy";

    /// <summary>
    /// Merkezi CORS politikasını DI konteynerine ekler.
    /// Yapılandırmada (appsettings/env) "Cors:AllowedOrigins" tanımlı ise yalnızca belirtilen domain'lere izin verir ve kimlik bilgilerini (credentials) destekler.
    /// Tanımlı origin bulunmuyorsa (yerel geliştirme/Swagger modu) tüm origin'lere esnek izin verir.
    /// </summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <param name="configuration">Uygulama konfigürasyonu.</param>
    /// <param name="policyName">CORS politika adı (varsayılan: DefaultCorsPolicy).</param>
    public static IServiceCollection AddSharedCors(
        this IServiceCollection services,
        IConfiguration configuration,
        string policyName = DefaultCorsPolicyName)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

        services.AddCors(options =>
        {
            options.AddPolicy(policyName, policy =>
            {
                if (allowedOrigins is { Length: > 0 })
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                }
                else
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                }
            });

            // Default policy olarak da tanımla ki parametresiz app.UseCors() ile doğrudan çalışabilsin
            options.AddDefaultPolicy(policy =>
            {
                if (allowedOrigins is { Length: > 0 })
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                }
                else
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                }
            });
        });

        return services;
    }

    /// <summary>
    /// Merkezi CORS middleware'ini HTTP request pipeline'ına ekler.
    /// </summary>
    /// <param name="app">Uygulama builder'ı.</param>
    /// <param name="policyName">Kullanılacak politika adı (isteğe bağlı).</param>
    public static IApplicationBuilder UseSharedCors(
        this IApplicationBuilder app,
        string? policyName = null)
    {
        return string.IsNullOrEmpty(policyName)
            ? app.UseCors()
            : app.UseCors(policyName);
    }
}
