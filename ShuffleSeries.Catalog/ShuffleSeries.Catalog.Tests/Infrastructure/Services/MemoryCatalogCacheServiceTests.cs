using Microsoft.Extensions.Caching.Memory;
using ShuffleSeries.Catalog.Infrastructure.Services;

namespace ShuffleSeries.Catalog.Tests.Infrastructure.Services;

public class MemoryCatalogCacheServiceTests : IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly MemoryCatalogCacheService _service;

    public MemoryCatalogCacheServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _service = new MemoryCatalogCacheService(_memoryCache);
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
    }

    [Fact]
    public async Task GetOrCreateAsync_Should_CallFactoryAndCache_WhenKeyDoesNotExist()
    {
        var callCount = 0;
        Task<string> Factory()
        {
            callCount++;
            return Task.FromResult("value1");
        }

        var result = await _service.GetOrCreateAsync("key1", Factory);

        result.Should().Be("value1");
        callCount.Should().Be(1);

        // İkinci çağrıda factory çağrılmamalı (cache hit)
        var cachedResult = await _service.GetOrCreateAsync("key1", Factory);
        cachedResult.Should().Be("value1");
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreateAsync_Should_PreventCacheStampede_WhenMultipleConcurrentRequestsHitSameKey()
    {
        var callCount = 0;
        var tcs = new TaskCompletionSource<string>();

        async Task<string> SlowFactory()
        {
            Interlocked.Increment(ref callCount);
            return await tcs.Task;
        }

        // 10 eşzamanlı istek aynı anda cache miss ile gelsin
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _service.GetOrCreateAsync("stampede-key", SlowFactory))
            .ToList();

        // Factory'nin tamamlanmasını sağla
        tcs.SetResult("concurrent-result");

        var results = await Task.WhenAll(tasks);

        // Tüm çağrılar aynı sonucu almalı
        results.Should().AllBe("concurrent-result");
        // Factory KESİNLİKLE sadece 1 kez çağrılmış olmalı (Thundering herd savunması)
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task Remove_Should_EvictKeyFromCache()
    {
        var callCount = 0;
        Task<string> Factory()
        {
            callCount++;
            return Task.FromResult($"val_{callCount}");
        }

        var result1 = await _service.GetOrCreateAsync("evict-key", Factory);
        result1.Should().Be("val_1");

        _service.Remove("evict-key");

        var result2 = await _service.GetOrCreateAsync("evict-key", Factory);
        result2.Should().Be("val_2");
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task RemoveAsync_Should_EvictKeyFromCache()
    {
        var callCount = 0;
        Task<string> Factory()
        {
            callCount++;
            return Task.FromResult($"val_{callCount}");
        }

        var result1 = await _service.GetOrCreateAsync("evict-async-key", Factory);
        result1.Should().Be("val_1");

        await _service.RemoveAsync("evict-async-key");

        var result2 = await _service.GetOrCreateAsync("evict-async-key", Factory);
        result2.Should().Be("val_2");
        callCount.Should().Be(2);
    }
}
