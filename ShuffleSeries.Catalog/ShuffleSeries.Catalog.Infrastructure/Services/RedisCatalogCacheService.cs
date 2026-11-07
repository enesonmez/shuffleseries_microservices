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

    private static readonly SemaphoreSlim[] _stripedLocks = Enumerable.Range(0, 64)
        .Select(_ => new SemaphoreSlim(1, 1))
        .ToArray();

    private static SemaphoreSlim GetLock(string key)
    {
        var hash = (uint)string.GetHashCode(key.AsSpan(), StringComparison.Ordinal);
        return _stripedLocks[hash % 64];
    }

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
        var (foundFast, fastCached) = await TryGetFromCacheAsync<T>(formattedKey, "fast-path");
        if (foundFast)
        {
            return fastCached!;
        }

        // 2. Slow path: Cache Stampede (Thundering Herd) savunması için per-key kilitleme (Striped Lock)
        var keyLock = GetLock(key);
        await keyLock.WaitAsync(cancellationToken);

        try
        {
            // Double-checked locking
            var (foundDoubleCheck, doubleCheckedCached) = await TryGetFromCacheAsync<T>(formattedKey, "double-check");
            if (foundDoubleCheck)
            {
                return doubleCheckedCached!;
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
                var serializedBytes = JsonSerializer.SerializeToUtf8Bytes(value, _jsonOptions);
                await db.StringSetAsync(formattedKey, serializedBytes, effectiveExpiration);
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

    private async Task<(bool Found, T? Value)> TryGetFromCacheAsync<T>(string formattedKey, string stage)
    {
        try
        {
            var db = _redis.GetDatabase();
            var cachedValue = await db.StringGetAsync(formattedKey);

            if (cachedValue.HasValue)
            {
                var bytes = (byte[])cachedValue!;
                var deserialized = JsonSerializer.Deserialize<T>(bytes, _jsonOptions);
                if (deserialized is not null)
                {
                    return (true, deserialized);
                }
            }
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException or System.Net.Sockets.SocketException)
        {
            _logger.LogWarning(ex, "Redis cache {Stage} read failed for key {CacheKey}.", stage, formattedKey);
        }

        return (false, default);
    }

    private string FormatKey(string key)
    {
        var prefix = _redisOptions.InstanceName ?? string.Empty;
        return $"{prefix}{key}";
    }
}
