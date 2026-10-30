using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Shared.Core.Infrastructure.Configuration.Redis;
using StackExchange.Redis;

namespace ShuffleSeries.Catalog.Infrastructure.Services;

internal sealed class RedisCatalogCacheService : ICatalogCacheService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IConnectionMultiplexer _redis;
    private readonly RedisOptions _redisOptions;
    private readonly ILogger<RedisCatalogCacheService> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public RedisCatalogCacheService(
        IConnectionMultiplexer redis,
        IOptions<RedisOptions> redisOptions,
        ILogger<RedisCatalogCacheService> logger)
    {
        _redis = redis;
        _redisOptions = redisOptions.Value;
        _logger = logger;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var formattedKey = FormatKey(key);

        // 1. Fast path: Redis'ten lock-free okuma
        try
        {
            var db = _redis.GetDatabase();
            var cachedValue = await db.StringGetAsync(formattedKey);

            if (cachedValue.HasValue)
            {
                var deserialized = JsonSerializer.Deserialize<T>(cachedValue.ToString(), _jsonOptions);
                if (deserialized is not null)
                {
                    return deserialized;
                }
            }
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException or System.Net.Sockets.SocketException)
        {
            _logger.LogWarning(ex, "Redis cache read failed for key {CacheKey}. Falling back to lock/DB.", formattedKey);
        }

        // 2. Slow path: Cache Stampede (Thundering Herd) savunması için per-key kilitleme
        var keyLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await keyLock.WaitAsync(cancellationToken);

        try
        {
            // Double-checked locking
            try
            {
                var db = _redis.GetDatabase();
                var cachedValue = await db.StringGetAsync(formattedKey);

                if (cachedValue.HasValue)
                {
                    var deserialized = JsonSerializer.Deserialize<T>(cachedValue.ToString(), _jsonOptions);
                    if (deserialized is not null)
                    {
                        return deserialized;
                    }
                }
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException or System.Net.Sockets.SocketException)
            {
                _logger.LogWarning(ex, "Redis double-check read failed for key {CacheKey}.", formattedKey);
            }

            // 3. Veritabanı Factory çağrısı
            var value = await factory();

            // 4. Cache Avalanche savunması: TTL Jitter (+ 0-180 saniye rastgele sapma)
            var baseExpiration = expiration ?? CatalogCacheKeys.DefaultMetadataExpiration;
            var jitterSeconds = Random.Shared.Next(0, 180);
            var effectiveExpiration = baseExpiration.Add(TimeSpan.FromSeconds(jitterSeconds));

            try
            {
                var db = _redis.GetDatabase();
                var serialized = JsonSerializer.Serialize(value, _jsonOptions);
                await db.StringSetAsync(formattedKey, serialized, effectiveExpiration);
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException or System.Net.Sockets.SocketException)
            {
                _logger.LogWarning(ex, "Redis cache write failed for key {CacheKey}. Continuing with DB value.", formattedKey);
            }

            return value;
        }
        finally
        {
            keyLock.Release();
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var formattedKey = FormatKey(key);
        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(formattedKey);
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException or System.Net.Sockets.SocketException)
        {
            _logger.LogWarning(ex, "Redis cache key removal failed for key {CacheKey}.", formattedKey);
        }
    }

    public void Remove(string key)
    {
        var formattedKey = FormatKey(key);
        try
        {
            var db = _redis.GetDatabase();
            db.KeyDelete(formattedKey);
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException or System.Net.Sockets.SocketException)
        {
            _logger.LogWarning(ex, "Redis sync cache key removal failed for key {CacheKey}.", formattedKey);
        }
    }

    private string FormatKey(string key)
    {
        var prefix = _redisOptions.InstanceName ?? string.Empty;
        return $"{prefix}{key}";
    }
}
