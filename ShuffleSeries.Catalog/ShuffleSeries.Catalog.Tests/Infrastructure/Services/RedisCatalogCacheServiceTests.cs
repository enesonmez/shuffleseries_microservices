using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using ShuffleSeries.Catalog.Infrastructure.Services;
using ShuffleSeries.Shared.Core.Infrastructure.Configuration.Redis;
using StackExchange.Redis;

namespace ShuffleSeries.Catalog.Tests.Infrastructure.Services;

public class RedisCatalogCacheServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly IOptions<RedisOptions> _options;
    private readonly RedisCatalogCacheService _service;

    public RedisCatalogCacheServiceTests()
    {
        _redisMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();
        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);

        _options = Options.Create(new RedisOptions
        {
            Host = "localhost",
            Port = 6379,
            InstanceName = "test:"
        });

        _service = new RedisCatalogCacheService(
            _redisMock.Object,
            _options,
            NullLogger<RedisCatalogCacheService>.Instance);
    }

    private record SampleDto(string Name, int Number);

    [Fact]
    public async Task GetOrCreateAsync_Should_ReturnCachedValue_WhenCacheHit()
    {
        var sampleData = new SampleDto("HitValue", 42);
        var serialized = JsonSerializer.Serialize(sampleData);

        _databaseMock.Setup(db => db.StringGetAsync("test:sample-key", It.IsAny<CommandFlags>()))
            .ReturnsAsync(serialized);

        var factoryCalled = false;
        Task<SampleDto> Factory()
        {
            factoryCalled = true;
            return Task.FromResult(new SampleDto("DbValue", 99));
        }

        var result = await _service.GetOrCreateAsync("sample-key", Factory);

        result.Should().NotBeNull();
        result.Name.Should().Be("HitValue");
        result.Number.Should().Be(42);
        factoryCalled.Should().BeFalse();
        _databaseMock.Verify(db => db.StringGetAsync("test:sample-key", It.IsAny<CommandFlags>()), Times.Once);
        _databaseMock.Verify(db => db.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateAsync_Should_CallFactoryAndCache_WhenCacheMiss()
    {
        _databaseMock.Setup(db => db.StringGetAsync("test:miss-key", It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        _databaseMock.Setup(db => db.StringSetAsync(
                "test:miss-key",
                It.IsAny<RedisValue>(),
                It.IsAny<Expiration>(),
                It.IsAny<ValueCondition>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var callCount = 0;
        Task<SampleDto> Factory()
        {
            callCount++;
            return Task.FromResult(new SampleDto("CreatedValue", 100));
        }

        var result = await _service.GetOrCreateAsync("miss-key", Factory);

        result.Should().NotBeNull();
        result.Name.Should().Be("CreatedValue");
        callCount.Should().Be(1);

        _databaseMock.Verify(db => db.StringSetAsync(
            "test:miss-key",
            It.Is<RedisValue>(v => v.ToString().Contains("CreatedValue")),
            It.IsAny<Expiration>(),
            It.IsAny<ValueCondition>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateAsync_Should_PreventCacheStampede_WhenMultipleConcurrentRequestsHitSameKey()
    {
        RedisValue storedValue = RedisValue.Null;

        _databaseMock.Setup(db => db.StringGetAsync("test:stampede-key", It.IsAny<CommandFlags>()))
            .ReturnsAsync(() => storedValue);

        _databaseMock.Setup(db => db.StringSetAsync(
                "test:stampede-key",
                It.IsAny<RedisValue>(),
                It.IsAny<Expiration>(),
                It.IsAny<ValueCondition>(),
                It.IsAny<CommandFlags>()))
            .Callback<RedisKey, RedisValue, Expiration, ValueCondition, CommandFlags>((_, v, _, _, _) => storedValue = v)
            .ReturnsAsync(true);

        var callCount = 0;
        var tcs = new TaskCompletionSource<SampleDto>();

        async Task<SampleDto> SlowFactory()
        {
            Interlocked.Increment(ref callCount);
            return await tcs.Task;
        }

        var tasks = Enumerable.Range(0, 5)
            .Select(_ => _service.GetOrCreateAsync("stampede-key", SlowFactory))
            .ToList();

        tcs.SetResult(new SampleDto("StampedeResult", 777));
        var results = await Task.WhenAll(tasks);

        results.Should().AllSatisfy(r =>
        {
            r.Name.Should().Be("StampedeResult");
            r.Number.Should().Be(777);
        });

        callCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreateAsync_Should_FallbackToFactory_WhenRedisThrowsException()
    {
        _databaseMock.Setup(db => db.StringGetAsync("test:error-key", It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis is down"));

        var factoryCalled = false;
        Task<SampleDto> Factory()
        {
            factoryCalled = true;
            return Task.FromResult(new SampleDto("FallbackValue", 500));
        }

        var result = await _service.GetOrCreateAsync("error-key", Factory);

        result.Should().NotBeNull();
        result.Name.Should().Be("FallbackValue");
        factoryCalled.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveAsync_Should_DeleteKeyFromRedis()
    {
        _databaseMock.Setup(db => db.KeyDeleteAsync("test:remove-key", CommandFlags.None))
            .ReturnsAsync(true);

        await _service.RemoveAsync("remove-key");

        _databaseMock.Verify(db => db.KeyDeleteAsync("test:remove-key", CommandFlags.None), Times.Once);
    }

    [Fact]
    public void Remove_Should_CallKeyDelete()
    {
        _databaseMock.Setup(db => db.KeyDelete("test:sync-key", CommandFlags.None))
            .Returns(true);

        _service.Remove("sync-key");

        _databaseMock.Verify(db => db.KeyDelete("test:sync-key", CommandFlags.None), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_Should_NotThrow_WhenRedisFails()
    {
        _databaseMock.Setup(db => db.KeyDeleteAsync("test:fail-key", CommandFlags.None))
            .ThrowsAsync(new RedisTimeoutException("Redis timeout", CommandStatus.WaitingToBeSent));

        var act = async () => await _service.RemoveAsync("fail-key");

        await act.Should().NotThrowAsync();
    }
}
