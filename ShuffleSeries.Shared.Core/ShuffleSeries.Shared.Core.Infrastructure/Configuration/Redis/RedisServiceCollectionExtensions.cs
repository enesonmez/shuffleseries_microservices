using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ShuffleSeries.Shared.Core.Application.Security;
using ShuffleSeries.Shared.Core.Infrastructure.Services.Security;
using StackExchange.Redis;

namespace ShuffleSeries.Shared.Core.Infrastructure.Configuration.Redis;

/// <summary>
/// Redis ve Token Blacklist bağımlılık enjeksiyonu extension metotları.
/// </summary>
public static class RedisServiceCollectionExtensions
{
    /// <summary>
    /// Singleton StackExchange.Redis IConnectionMultiplexer bağlantı havuzunu kaydeder.
    /// </summary>
    public static IServiceCollection AddSharedRedis(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));

        services.TryAddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
            var connectionString = options.ToConnectionString();

            var configurationOptions = ConfigurationOptions.Parse(connectionString);
            configurationOptions.AbortOnConnectFail = options.AbortOnConnectFail;
            configurationOptions.ConnectTimeout = options.ConnectTimeout;
            configurationOptions.SyncTimeout = options.SyncTimeout;

            return ConnectionMultiplexer.Connect(configurationOptions);
        });

        return services;
    }

    /// <summary>
    /// ITokenBlacklistService uygulamasını (RedisTokenBlacklistService) DI konteynerine kaydeder.
    /// Bağımsız ve açık bağımlılık kullanımı için öncesinde services.AddSharedRedis(configuration) çağrılmış olmalıdır.
    /// </summary>
    public static IServiceCollection AddSharedTokenBlacklist(this IServiceCollection services)
    {
        services.TryAddSingleton<ITokenBlacklistService, RedisTokenBlacklistService>();
        return services;
    }

    /// <summary>
    /// ITokenBlacklistService uygulamasını (RedisTokenBlacklistService) DI konteynerine kaydeder.
    /// Geriye dönük uyumluluk için configuration ile birlikte Redis kaydını da destekler.
    /// </summary>
    public static IServiceCollection AddSharedTokenBlacklist(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSharedRedis(configuration);
        services.TryAddSingleton<ITokenBlacklistService, RedisTokenBlacklistService>();

        return services;
    }
}
