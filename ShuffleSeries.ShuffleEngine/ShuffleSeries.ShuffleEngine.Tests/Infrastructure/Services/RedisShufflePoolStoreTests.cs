using Microsoft.Extensions.Options;
using Moq;
using ShuffleSeries.Shared.Core.Infrastructure.Configuration.Redis;
using ShuffleSeries.ShuffleEngine.Domain.Enums;
using ShuffleSeries.ShuffleEngine.Domain.Models;
using ShuffleSeries.ShuffleEngine.Infrastructure.Services;
using StackExchange.Redis;

namespace ShuffleSeries.ShuffleEngine.Tests.Infrastructure.Services;

public class RedisShufflePoolStoreTests
{
    private static ShuffleMediaSnapshot CreateMovie(
        Guid id,
        double imdbRating = 8.5,
        IReadOnlyList<Guid>? platforms = null,
        IReadOnlyList<Guid>? moods = null,
        IReadOnlyList<Guid>? genres = null,
        bool safetyNet = true) =>
        new(
            Id: id,
            Kind: ShuffleMediaKind.Movie,
            TmdbId: 100,
            ImdbId: "tt0000100",
            ImdbRating: imdbRating,
            ImdbVotes: 150_000,
            ReleaseYear: 2020,
            PosterUrl: "https://img/poster.jpg",
            BackdropUrl: null,
            RuntimeMinutes: 120,
            Director: "Denis Villeneuve",
            SeriesType: null,
            TotalSeasons: null,
            BaseQualityScore: 8.4,
            IsSafetyNetEligible: safetyNet,
            PlatformIds: platforms ?? [],
            MoodIds: moods ?? [],
            GenreIds: genres ?? [],
            PlatformLinks: [],
            Translations: new Dictionary<string, ShuffleLocalizedText> { ["en"] = new("Dune", "Sci-fi") });

    [Fact]
    public async Task UpsertMediaBatchAsync_WhenIncomingEventIsOlderThanStoredTicks_ShouldSkipWrite()
    {
        var mediaId = Guid.NewGuid();
        var movie = CreateMovie(mediaId);
        var newerTicks = new DateTime(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc).Ticks;
        var olderTicks = new DateTime(2026, 11, 27, 11, 0, 0, DateTimeKind.Utc).Ticks;

        var readBatchMock = new Mock<IBatch>();
        readBatchMock
            .Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync([newerTicks.ToString(), "1", "", "", "", "Denis Villeneuve"]);

        var writeBatchMock = new Mock<IBatch>();

        var dbMock = new Mock<IDatabase>();
        dbMock.SetupSequence(d => d.CreateBatch(It.IsAny<object?>()))
            .Returns(readBatchMock.Object)
            .Returns(writeBatchMock.Object);

        var redisMock = new Mock<IConnectionMultiplexer>();
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(dbMock.Object);

        var store = new RedisShufflePoolStore(redisMock.Object, Options.Create(new RedisOptions { InstanceName = "test:" }));

        await store.UpsertMediaBatchAsync([movie], olderTicks);

        writeBatchMock.Verify(
            b => b.HashSetAsync(It.IsAny<RedisKey>(), It.IsAny<HashEntry[]>(), It.IsAny<CommandFlags>()),
            Times.Never);
        writeBatchMock.Verify(
            b => b.SortedSetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<double>(), It.IsAny<SortedSetWhen>(), It.IsAny<CommandFlags>()),
            Times.Never);
    }

    [Fact]
    public async Task UpsertMediaBatchAsync_WhenPlatformRemovedAndRatingDropped_ShouldCleanStaleIndexesAndSafetyNet()
    {
        var mediaId = Guid.NewGuid();
        var oldPlatformId = Guid.NewGuid();
        var keptPlatformId = Guid.NewGuid();
        var oldTicks = new DateTime(2026, 11, 27, 10, 0, 0, DateTimeKind.Utc).Ticks;
        var newTicks = new DateTime(2026, 11, 27, 11, 0, 0, DateTimeKind.Utc).Ticks;

        var movie = CreateMovie(mediaId, imdbRating: 6.8, platforms: [keptPlatformId], safetyNet: false);

        var readBatchMock = new Mock<IBatch>();
        readBatchMock
            .Setup(b => b.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync([oldTicks.ToString(), "1", $"{oldPlatformId},{keptPlatformId}", "", "", "Denis Villeneuve"]);

        var writeBatchMock = new Mock<IBatch>();
        writeBatchMock.Setup(b => b.HashSetAsync(It.IsAny<RedisKey>(), It.IsAny<HashEntry[]>(), It.IsAny<CommandFlags>())).Returns(Task.CompletedTask);
        writeBatchMock.Setup(b => b.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan?>(), It.IsAny<ExpireWhen>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
        writeBatchMock.Setup(b => b.SortedSetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<double>(), It.IsAny<SortedSetWhen>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
        writeBatchMock.Setup(b => b.SortedSetRemoveAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
        writeBatchMock.Setup(b => b.SetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
        writeBatchMock.Setup(b => b.SetRemoveAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);

        var dbMock = new Mock<IDatabase>();
        dbMock.SetupSequence(d => d.CreateBatch(It.IsAny<object?>()))
            .Returns(readBatchMock.Object)
            .Returns(writeBatchMock.Object);

        var redisMock = new Mock<IConnectionMultiplexer>();
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(dbMock.Object);

        var store = new RedisShufflePoolStore(redisMock.Object, Options.Create(new RedisOptions { InstanceName = "test:" }));

        await store.UpsertMediaBatchAsync([movie], newTicks);

        writeBatchMock.Verify(
            b => b.SetRemoveAsync(
                (RedisKey)$"test:shuffle:idx:platform:{oldPlatformId}:movies",
                (RedisValue)mediaId.ToString(),
                It.IsAny<CommandFlags>()),
            Times.Once);
        writeBatchMock.Verify(
            b => b.SetRemoveAsync(
                (RedisKey)$"test:shuffle:idx:platform:{keptPlatformId}:movies",
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()),
            Times.Never);
        writeBatchMock.Verify(
            b => b.SortedSetRemoveAsync(
                (RedisKey)"test:shuffle:safety-net:movies",
                (RedisValue)mediaId.ToString(),
                It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveTaxonomyIndexAsync_WhenValidTaxonomy_ShouldDeleteAllThreeKindSets()
    {
        var moodId = Guid.NewGuid();
        var dbMock = new Mock<IDatabase>();
        dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>())).ReturnsAsync(3);

        var redisMock = new Mock<IConnectionMultiplexer>();
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(dbMock.Object);

        var store = new RedisShufflePoolStore(redisMock.Object, Options.Create(new RedisOptions { InstanceName = "test:" }));

        await store.RemoveTaxonomyIndexAsync("Mood", moodId);

        dbMock.Verify(
            d => d.KeyDeleteAsync(
                It.Is<RedisKey[]>(keys =>
                    keys.Length == 3 &&
                    keys.Contains((RedisKey)$"test:shuffle:idx:mood:{moodId}:movies") &&
                    keys.Contains((RedisKey)$"test:shuffle:idx:mood:{moodId}:series") &&
                    keys.Contains((RedisKey)$"test:shuffle:idx:mood:{moodId}:episodes")),
                It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveTaxonomyIndexAsync_WhenUnknownTaxonomy_ShouldNotCallRedis()
    {
        var dbMock = new Mock<IDatabase>();
        var redisMock = new Mock<IConnectionMultiplexer>();
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(dbMock.Object);

        var store = new RedisShufflePoolStore(redisMock.Object, Options.Create(new RedisOptions { InstanceName = "test:" }));

        await store.RemoveTaxonomyIndexAsync("UnknownType", Guid.NewGuid());

        dbMock.Verify(d => d.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task GetMediaAffinityContextAsync_WhenHashExists_ShouldParseMoodsGenresAndDirector()
    {
        var mediaId = Guid.NewGuid();
        var moodId = Guid.NewGuid();
        var genreId = Guid.NewGuid();

        var dbMock = new Mock<IDatabase>();
        dbMock.Setup(d => d.HashGetAsync(
                (RedisKey)$"test:shuffle:media:{mediaId}",
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync([moodId.ToString(), genreId.ToString(), "Christopher Nolan"]);

        var redisMock = new Mock<IConnectionMultiplexer>();
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(dbMock.Object);

        var store = new RedisShufflePoolStore(redisMock.Object, Options.Create(new RedisOptions { InstanceName = "test:" }));

        var ctx = await store.GetMediaAffinityContextAsync(mediaId);

        ctx.Should().NotBeNull();
        ctx!.MediaId.Should().Be(mediaId);
        ctx.MoodIds.Should().ContainSingle().Which.Should().Be(moodId);
        ctx.GenreIds.Should().ContainSingle().Which.Should().Be(genreId);
        ctx.Director.Should().Be("Christopher Nolan");
    }

    [Fact]
    public async Task PruneStaleEntriesAsync_ShouldEvictOnlyEntriesOlderThanMinOccurredAtTicks()
    {
        var freshId = Guid.NewGuid();
        var staleId = Guid.NewGuid();
        var stalePlatformId = Guid.NewGuid();
        var syncTicks = new DateTime(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc).Ticks;
        var oldTicks = syncTicks - TimeSpan.FromMinutes(10).Ticks;

        var poolReadBatch = new Mock<IBatch>();
        poolReadBatch
            .Setup(b => b.SortedSetRangeByRankAsync((RedisKey)"test:shuffle:pool:movies", 0, -1, Order.Ascending, CommandFlags.None))
            .ReturnsAsync([freshId.ToString(), staleId.ToString()]);
        poolReadBatch
            .Setup(b => b.SortedSetRangeByRankAsync((RedisKey)"test:shuffle:pool:series", 0, -1, Order.Ascending, CommandFlags.None))
            .ReturnsAsync([]);
        poolReadBatch
            .Setup(b => b.SortedSetRangeByRankAsync((RedisKey)"test:shuffle:pool:episodes", 0, -1, Order.Ascending, CommandFlags.None))
            .ReturnsAsync([]);

        var metaBatch = new Mock<IBatch>();
        metaBatch
            .Setup(b => b.HashGetAsync((RedisKey)$"test:shuffle:media:{freshId}", It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync([syncTicks.ToString(), "1", "", "", "", ""]);
        metaBatch
            .Setup(b => b.HashGetAsync((RedisKey)$"test:shuffle:media:{staleId}", It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync([oldTicks.ToString(), "1", stalePlatformId.ToString(), "", "", ""]);

        var writeBatch = new Mock<IBatch>();
        writeBatch.Setup(b => b.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
        writeBatch.Setup(b => b.SortedSetRemoveAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
        writeBatch.Setup(b => b.SetRemoveAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);

        var dbMock = new Mock<IDatabase>();
        dbMock.SetupSequence(d => d.CreateBatch(It.IsAny<object?>()))
            .Returns(poolReadBatch.Object)
            .Returns(metaBatch.Object)
            .Returns(writeBatch.Object);

        var redisMock = new Mock<IConnectionMultiplexer>();
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(dbMock.Object);

        var store = new RedisShufflePoolStore(redisMock.Object, Options.Create(new RedisOptions { InstanceName = "test:" }));

        await store.PruneStaleEntriesAsync(syncTicks);

        writeBatch.Verify(b => b.KeyDeleteAsync((RedisKey)$"test:shuffle:media:{staleId}", It.IsAny<CommandFlags>()), Times.Once);
        writeBatch.Verify(b => b.KeyDeleteAsync((RedisKey)$"test:shuffle:media:{freshId}", It.IsAny<CommandFlags>()), Times.Never);
        writeBatch.Verify(b => b.SortedSetRemoveAsync((RedisKey)"test:shuffle:pool:movies", (RedisValue)staleId.ToString(), It.IsAny<CommandFlags>()), Times.Once);
        writeBatch.Verify(b => b.SetRemoveAsync((RedisKey)$"test:shuffle:idx:platform:{stalePlatformId}:movies", (RedisValue)staleId.ToString(), It.IsAny<CommandFlags>()), Times.Once);
    }
}
