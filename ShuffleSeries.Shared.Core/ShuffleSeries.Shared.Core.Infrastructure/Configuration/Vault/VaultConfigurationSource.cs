using Microsoft.Extensions.Configuration;

namespace ShuffleSeries.Shared.Core.Infrastructure.Configuration.Vault;

/// <summary>
/// HashiCorp Vault için IConfigurationSource implementasyonu.
/// </summary>
public sealed class VaultConfigurationSource : IConfigurationSource
{
    public VaultConfigurationOptions Options { get; }
    public HttpMessageHandler? Handler { get; set; }

    public VaultConfigurationSource(VaultConfigurationOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new VaultConfigurationProvider(Options, Handler);
    }
}
