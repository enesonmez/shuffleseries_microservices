using Microsoft.Extensions.Configuration;

namespace ShuffleSeries.Shared.Core.Infrastructure.Configuration.Vault;

/// <summary>
/// IConfigurationBuilder için HashiCorp Vault genişletme metotları.
/// </summary>
public static class VaultExtensions
{
    /// <summary>
    /// HashiCorp Vault konfigürasyon sağlayıcısını IConfigurationBuilder'a ekler.
    /// </summary>
    /// <param name="builder">Konfigürasyon builder nesnesi.</param>
    /// <param name="configure">Vault seçeneklerini yapılandıran delege.</param>
    /// <param name="handler">Opsiyonel HttpMessageHandler (özellikle test senaryoları için).</param>
    public static IConfigurationBuilder AddVault(
        this IConfigurationBuilder builder,
        Action<VaultConfigurationOptions> configure,
        HttpMessageHandler? handler = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new VaultConfigurationOptions();
        configure(options);

        var source = new VaultConfigurationSource(options)
        {
            Handler = handler
        };

        return builder.Add(source);
    }

    /// <summary>
    /// Mevcut IConfiguration ve Environment değişkenlerini baz alarak HashiCorp Vault sağlayıcısını ekler.
    /// </summary>
    /// <param name="builder">Konfigürasyon builder nesnesi.</param>
    /// <param name="serviceName">İlgili mikroservisin adı (örn. 'catalog'). 'shuffleseries/shared' ve 'shuffleseries/{serviceName}' yolları otomatik eklenir.</param>
    /// <param name="handler">Opsiyonel HttpMessageHandler.</param>
    public static IConfigurationBuilder AddVault(
        this IConfigurationBuilder builder,
        string? serviceName = null,
        HttpMessageHandler? handler = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Mevcut geçici konfigürasyonu derleyerek Vault ayarlarını oku
        var tempConfig = builder.Build();
        var vaultSection = tempConfig.GetSection(VaultConfigurationOptions.SectionName);

        var options = new VaultConfigurationOptions
        {
            Address = Environment.GetEnvironmentVariable("VAULT_ADDR")
                      ?? vaultSection["Address"]
                      ?? "http://localhost:8200",

            Token = Environment.GetEnvironmentVariable("VAULT_TOKEN")
                    ?? Environment.GetEnvironmentVariable("VAULT_ROOT_TOKEN")
                    ?? vaultSection["Token"]
                    ?? "root",

            MountPoint = vaultSection["MountPoint"] ?? "secret",

            Enabled = bool.TryParse(Environment.GetEnvironmentVariable("VAULT_ENABLED") ?? vaultSection["Enabled"], out var enabled)
                ? enabled
                : true,

            Optional = bool.TryParse(Environment.GetEnvironmentVariable("VAULT_OPTIONAL") ?? vaultSection["Optional"], out var optional)
                ? optional
                : true
        };

        // Konfigürasyondan belirtilen yolları al
        var configuredPaths = vaultSection.GetSection("Paths").Get<List<string>>() ?? new List<string>();
        foreach (var path in configuredPaths)
        {
            if (!string.IsNullOrWhiteSpace(path) && !options.Paths.Contains(path))
            {
                options.Paths.Add(path);
            }
        }

        // Standart hiyerarşik yollar (Shared ve Servise Özel)
        if (!options.Paths.Contains("shuffleseries/shared"))
        {
            options.Paths.Add("shuffleseries/shared");
        }

        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            var servicePath = $"shuffleseries/{serviceName.ToLowerInvariant()}";
            if (!options.Paths.Contains(servicePath))
            {
                options.Paths.Add(servicePath);
            }
        }

        var source = new VaultConfigurationSource(options)
        {
            Handler = handler
        };

        return builder.Add(source);
    }
}
