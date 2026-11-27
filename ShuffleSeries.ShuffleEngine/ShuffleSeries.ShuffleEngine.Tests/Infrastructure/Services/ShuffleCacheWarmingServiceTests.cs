using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using ShuffleSeries.Shared.Core.Infrastructure.Configuration.Redis;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Application.Common.Models;
using ShuffleSeries.ShuffleEngine.Domain.Enums;
using ShuffleSeries.ShuffleEngine.Domain.Models;
using ShuffleSeries.ShuffleEngine.Infrastructure.Services;
using StackExchange.Redis;

namespace ShuffleSeries.ShuffleEngine.Tests.Infrastructure.Services;

public class ShuffleCacheWarmingServiceTests
{
    [Fact]
    public async Task EnsureWarmedAsync_WhenAlreadyWarmedAndNotForced_ShouldSkipImmediately()
    {
        var now = DateTime.UtcNow;
        var poolMock = new Mock<IShufflePoolStore>();
        poolMock.Setup(x => x.IsPoolWarmedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        poolMock.Setup(x => x.GetPoolStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShufflePoolStatistics(true, 5, 3, 10, 4, 2, 8, now));

        var catalogMock = new Mock<ICatalogSyncGrpcClient>();
        var redisMock = new Mock<IConnectionMultiplexer>();

        var service = new ShuffleCacheWarmingService(
            poolMock.Object,
            catalogMock.Object,
            redisMock.Object,
            Options.Create(new RedisOptions()),
            TimeProvider.System,
            NullLogger<ShuffleCacheWarmingService>.Instance);

        var result = await service.EnsureWarmedAsync(forceRebuild: false);

        result.WasSkipped.Should().BeTrue();
        result.MoviesSynced.Should().Be(5);
        result.SeriesSynced.Should().Be(3);
        result.StandaloneEpisodesSynced.Should().Be(10);
        result.SafetyNetItemsCount.Should().Be(14);
        catalogMock.Verify(x => x.GetBulkMediaPageAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnsureWarmedAsync_WhenColdStart_ShouldAcquireLockAndStreamAllPages()
    {
        var poolMock = new Mock<IShufflePoolStore>();
        poolMock.Setup(x => x.IsPoolWarmedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var movie = new ShuffleMediaSnapshot(
            Guid.NewGuid(), ShuffleMediaKind.Movie, 27205, "tt1375666", 8.8, 2_500_000, 2010,
            "https://img/inc.jpg", null, 148, "Christopher Nolan", null, null, 8.9, IsSafetyNetEligible: true,
            [], [], [], [], new Dictionary<string, ShuffleLocalizedText>());

        var episode = new ShuffleEpisodeSnapshot(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, 4, 61, 8.6, null, "https://img/bm.jpg", 2016,
            8.4, IsSafetyNetEligible: true, [], [], [], [],
            new Dictionary<string, ShuffleLocalizedText>(),
            new Dictionary<string, ShuffleLocalizedText>());

        var catalogMock = new Mock<ICatalogSyncGrpcClient>();
        catalogMock.Setup(x => x.GetBulkMediaPageAsync(1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CatalogPagedResult<ShuffleMediaSnapshot>([movie], 1, 1, 100));
        catalogMock.Setup(x => x.GetStandaloneEpisodesPageAsync(1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CatalogPagedResult<ShuffleEpisodeSnapshot>([episode], 1, 1, 100));

        var dbMock = new Mock<IDatabase>();
        dbMock.Setup(x => x.LockTakeAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        dbMock.Setup(x => x.LockReleaseAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var redisMock = new Mock<IConnectionMultiplexer>();
        redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

        var service = new ShuffleCacheWarmingService(
            poolMock.Object,
            catalogMock.Object,
            redisMock.Object,
            Options.Create(new RedisOptions()),
            TimeProvider.System,
            NullLogger<ShuffleCacheWarmingService>.Instance);

        var result = await service.EnsureWarmedAsync(forceRebuild: false);

        result.WasSkipped.Should().BeFalse();
        result.MoviesSynced.Should().Be(1);
        result.SeriesSynced.Should().Be(0);
        result.StandaloneEpisodesSynced.Should().Be(1);
        result.SafetyNetItemsCount.Should().Be(2);

        poolMock.Verify(x => x.UpsertMediaBatchAsync(It.IsAny<IReadOnlyList<ShuffleMediaSnapshot>>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
        poolMock.Verify(x => x.UpsertEpisodesBatchAsync(It.IsAny<IReadOnlyList<ShuffleEpisodeSnapshot>>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
        poolMock.Verify(x => x.PruneStaleEntriesAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        poolMock.Verify(x => x.MarkPoolWarmedAsync(It.IsAny<ShufflePoolWarmState>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnsureWarmedAsync_WhenForceRebuildAndLockAlreadyHeld_ShouldSkipWithoutBypassingDistributedLock()
    {
        var now = DateTime.UtcNow;
        var poolMock = new Mock<IShufflePoolStore>();
        poolMock.Setup(x => x.GetPoolStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShufflePoolStatistics(true, 2, 1, 4, 2, 1, 3, now));

        var catalogMock = new Mock<ICatalogSyncGrpcClient>();

        var dbMock = new Mock<IDatabase>();
        dbMock.Setup(x => x.LockTakeAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        var redisMock = new Mock<IConnectionMultiplexer>();
        redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

        var service = new ShuffleCacheWarmingService(
            poolMock.Object,
            catalogMock.Object,
            redisMock.Object,
            Options.Create(new RedisOptions()),
            TimeProvider.System,
            NullLogger<ShuffleCacheWarmingService>.Instance);

        var result = await service.EnsureWarmedAsync(forceRebuild: true);

        result.WasSkipped.Should().BeTrue();
        catalogMock.Verify(x => x.GetBulkMediaPageAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        poolMock.Verify(x => x.ClearWarmMarkerAsync(It.IsAny<CancellationToken>()), Times.Never);
        dbMock.Verify(x => x.LockReleaseAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task EnsureWarmedAsync_WhenForceRebuildAndLockAcquired_ShouldClearMarkerAndPruneStaleEntries()
    {
        var poolMock = new Mock<IShufflePoolStore>();
        var catalogMock = new Mock<ICatalogSyncGrpcClient>();
        catalogMock.Setup(x => x.GetBulkMediaPageAsync(1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CatalogPagedResult<ShuffleMediaSnapshot>([], 0, 1, 100));
        catalogMock.Setup(x => x.GetStandaloneEpisodesPageAsync(1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CatalogPagedResult<ShuffleEpisodeSnapshot>([], 0, 1, 100));

        var dbMock = new Mock<IDatabase>();
        dbMock.Setup(x => x.LockTakeAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
        dbMock.Setup(x => x.LockReleaseAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);

        var redisMock = new Mock<IConnectionMultiplexer>();
        redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

        var service = new ShuffleCacheWarmingService(
            poolMock.Object,
            catalogMock.Object,
            redisMock.Object,
            Options.Create(new RedisOptions()),
            TimeProvider.System,
            NullLogger<ShuffleCacheWarmingService>.Instance);

        var result = await service.EnsureWarmedAsync(forceRebuild: true);

        result.WasSkipped.Should().BeFalse();
        poolMock.Verify(x => x.ClearWarmMarkerAsync(It.IsAny<CancellationToken>()), Times.Once);
        poolMock.Verify(x => x.PruneStaleEntriesAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
        poolMock.Verify(x => x.MarkPoolWarmedAsync(It.IsAny<ShufflePoolWarmState>(), It.IsAny<CancellationToken>()), Times.Once);
        dbMock.Verify(x => x.LockReleaseAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()), Times.Once);
    }
}
