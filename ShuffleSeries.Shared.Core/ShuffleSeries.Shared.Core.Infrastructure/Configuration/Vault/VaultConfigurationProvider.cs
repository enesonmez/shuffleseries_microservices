using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace ShuffleSeries.Shared.Core.Infrastructure.Configuration.Vault;

/// <summary>
/// HashiCorp Vault KV v2 secret engine üzerinden sırları okuyan özel .NET Configuration Provider.
/// </summary>
public class VaultConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly VaultConfigurationOptions _options;
    private readonly HttpMessageHandler? _handler;
    private HttpClient? _httpClient;
    private bool _disposed;

    public VaultConfigurationProvider(VaultConfigurationOptions options, HttpMessageHandler? handler = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _handler = handler;
    }

    public override void Load()
    {
        if (!_options.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.Address))
        {
            if (_options.Optional)
            {
                return;
            }

            throw new InvalidOperationException("Vault Address cannot be null or empty when Vault is enabled.");
        }

        if (_options.Paths.Count == 0)
        {
            return;
        }

        _httpClient ??= _handler != null ? new HttpClient(_handler, disposeHandler: false) : new HttpClient();
        _httpClient.BaseAddress = new Uri(_options.Address.TrimEnd('/') + "/");
        _httpClient.Timeout = _options.Timeout;

        foreach (var path in _options.Paths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            LoadSecretsFromPath(path);
        }
    }

    private void LoadSecretsFromPath(string path)
    {
        var trimmedPath = path.Trim('/');
        // Vault KV-v2 HTTP endpoint formatı: v1/{mount}/data/{path}
        var endpoint = $"v1/{_options.MountPoint.Trim('/')}/data/{trimmedPath}";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Add("X-Vault-Token", _options.Token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = _httpClient!.Send(request);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                if (_options.Optional)
                {
                    return;
                }

                throw new InvalidOperationException($"Vault secret path '{path}' was not found at '{endpoint}'.");
            }

            response.EnsureSuccessStatusCode();

            using var stream = response.Content.ReadAsStream();
            using var jsonDoc = JsonDocument.Parse(stream);

            ParseVaultResponse(jsonDoc);
        }
        catch (Exception ex) when (_options.Optional && ex is not InvalidOperationException)
        {
            // İsteğe bağlı (optional) modda Vault ulaşılamazsa veya hata verirse
            // uygulamanın yerel yapılandırma ile devam etmesine izin verilir.
            Console.WriteLine($"[VaultConfigurationProvider] Warning: Could not read secret path '{path}' from Vault: {ex.Message}");
        }
    }

    internal void ParseVaultResponse(JsonDocument jsonDoc)
    {
        // HashiCorp Vault KV v2 yanıt formatı:
        // { "data": { "data": { "Key": "Value", ... } } }
        if (!jsonDoc.RootElement.TryGetProperty("data", out var dataWrapper) ||
            !dataWrapper.TryGetProperty("data", out var secretsElement))
        {
            return;
        }

        FlattenElement(string.Empty, secretsElement);
    }

    private void FlattenElement(string prefix, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    // Anahtardaki '__' ayraçlarını standart .NET ':' hiyerarşisine normalize et
                    var normalizedName = property.Name.Replace("__", ConfigurationPath.KeyDelimiter);
                    var childPrefix = string.IsNullOrEmpty(prefix)
                        ? normalizedName
                        : $"{prefix}{ConfigurationPath.KeyDelimiter}{normalizedName}";

                    FlattenElement(childPrefix, property.Value);
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var childPrefix = $"{prefix}{ConfigurationPath.KeyDelimiter}{index++}";
                    FlattenElement(childPrefix, item);
                }
                break;

            default:
                if (!string.IsNullOrEmpty(prefix))
                {
                    Data[prefix] = element.ToString();
                }
                break;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
            }

            _disposed = true;
        }
    }
}
