using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;

namespace ShuffleSeries.Catalog.Infrastructure.Services;

internal sealed class MemoryCatalogCacheService : ICatalogCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public MemoryCatalogCacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Fast path: Cache hit (lock-free)
        if (_memoryCache.TryGetValue(key, out T? cachedValue) && cachedValue is not null)
        {
            return cachedValue;
        }

        // 2. Slow path: Cache Stampede (Thundering Herd) savunması için per-key kilitleme
        var keyLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await keyLock.WaitAsync(cancellationToken);

        try
        {
            // Double-checked locking
            if (_memoryCache.TryGetValue(key, out cachedValue) && cachedValue is not null)
            {
                return cachedValue;
            }

            var value = await factory();

            // Cache Avalanche savunması: TTL Jitter (+ 0-180 saniye rastgele sapma)
            var baseExpiration = expiration ?? CatalogCacheKeys.DefaultMetadataExpiration;
            var jitterSeconds = Random.Shared.Next(0, 180);
            var effectiveExpiration = baseExpiration.Add(TimeSpan.FromSeconds(jitterSeconds));

            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = effectiveExpiration
            };

            _memoryCache.Set(key, value, cacheEntryOptions);
            return value;
        }
        finally
        {
            keyLock.Release();
        }
    }

    public void Remove(string key)
    {
        _memoryCache.Remove(key);
    }
}
