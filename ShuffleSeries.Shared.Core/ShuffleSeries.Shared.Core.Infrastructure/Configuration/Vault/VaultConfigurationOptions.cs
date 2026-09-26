namespace ShuffleSeries.Shared.Core.Infrastructure.Configuration.Vault;

/// <summary>
/// HashiCorp Vault bağlantı ve sır okuma seçeneklerini temsil eder.
/// </summary>
public sealed class VaultConfigurationOptions
{
    public const string SectionName = "Vault";

    /// <summary>
    /// Vault servisinin adresi (örn. http://localhost:8200 veya http://vault:8200).
    /// </summary>
    public string Address { get; set; } = "http://localhost:8200";

    /// <summary>
    /// Vault API erişim token'ı.
    /// </summary>
    public string Token { get; set; } = "root";

    /// <summary>
    /// KV secret engine mount noktası (varsayılan: secret).
    /// </summary>
    public string MountPoint { get; set; } = "secret";

    /// <summary>
    /// Okunacak sır yollarının listesi (örn. ["shuffleseries/shared", "shuffleseries/catalog"]).
    /// </summary>
    public IList<string> Paths { get; set; } = new List<string>();

    /// <summary>
    /// Vault entegrasyonunun aktif olup olmadığını belirler.
    /// False ise Vault çağrısı yapılmaz ve yerel yapılandırma kullanılır.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Vault erişilemez olduğunda veya sır bulunamadığında uygulamanın çökmesini engeller (fallback).
    /// Geliştirme ortamında veya Vault kapalıyken uygulamanın açılabilmesini sağlar.
    /// </summary>
    public bool Optional { get; set; } = true;

    /// <summary>
    /// HTTP istek zaman aşımı süresi.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
}
