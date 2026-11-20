using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ShuffleSeries.Shared.Core.Web.ForwardedHeaders;

public static class ForwardedHeadersExtensions
{
    /// <summary>
    /// Ters proxy (Reverse Proxy / Ingress / Load Balancer) arkasındaki gerçek istemci IP ve protokolünü
    /// güvenli bir şekilde çözen merkezi ForwardedHeaders yapılandırması.
    /// Spoofing'e karşı ForwardLimit = 1 sabitlemesi ve özel ağ CIDR tanımlarını içerir.
    /// </summary>
    public static IServiceCollection AddSharedForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                                     | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;

            // İstemcinin rastgele X-Forwarded-For dizisi enjekte etmesini önle
            options.ForwardLimit = 1;

            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();

            // Güvenli iç ağlar (Loopback + Private RFC 1918 & RFC 4193 CIDR'ları)
            options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Loopback, 8));
            options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.IPv6Loopback, 128));
            options.KnownIPNetworks.Add(new System.Net.IPNetwork(new IPAddress([10, 0, 0, 0]), 8));
            options.KnownIPNetworks.Add(new System.Net.IPNetwork(new IPAddress([172, 16, 0, 0]), 12));
            options.KnownIPNetworks.Add(new System.Net.IPNetwork(new IPAddress([192, 168, 0, 0]), 16));

            // Konfigürasyondan ek vekil sunucu / ağ IP'si varsa ekle (Dizi veya virgülle ayrılmış string desteği)
            var customProxies = configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>()
                ?? configuration["ForwardedHeaders:KnownProxies"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (customProxies is { Length: > 0 })
            {
                foreach (var proxy in customProxies)
                {
                    if (IPAddress.TryParse(proxy, out var ip))
                    {
                        options.KnownProxies.Add(ip);
                    }
                }
            }
        });

        return services;
    }
}
