namespace ShuffleSeries.Shared.Core.Infrastructure.Configuration.Redis;

/// <summary>
/// Redis bağlantı ve önbellekleme ayarları.
/// </summary>
public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    /// <summary>
    /// Redis host adresi (örn. 'redis' veya 'localhost').
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Redis port numarası (varsayılan: 6379).
    /// </summary>
    public int Port { get; set; } = 6379;

    /// <summary>
    /// Redis kimlik doğrulama şifresi.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Redis anahtar ön eki (varsayılan: 'shuffleseries:').
    /// </summary>
    public string InstanceName { get; set; } = "shuffleseries:";

    /// <summary>
    /// Bağlantı zaman aşımı süresi (milisaniye).
    /// </summary>
    public int ConnectTimeout { get; set; } = 5000;

    /// <summary>
    /// Senkron işlem zaman aşımı süresi (milisaniye).
    /// </summary>
    public int SyncTimeout { get; set; } = 1000;

    /// <summary>
    /// Başlangıçta Redis'e bağlanılamazsa uygulamanın çökmesini engellemek için false olmalıdır.
    /// </summary>
    public bool AbortOnConnectFail { get; set; } = false;

    /// <summary>
    /// StackExchange.Redis için bağlantı dizesini döndürür.
    /// </summary>
    public string ToConnectionString()
    {
        var connection = $"{Host}:{Port},abortConnect={AbortOnConnectFail},connectTimeout={ConnectTimeout},syncTimeout={SyncTimeout}";
        if (!string.IsNullOrWhiteSpace(Password))
        {
            connection += $",password={Password}";
        }
        return connection;
    }
}
