using Microsoft.Extensions.Options;
using Moq;
using ShuffleSeries.Shared.Core.Domain.Enums;
using ShuffleSeries.Shared.Core.Infrastructure.Configuration.Redis;
using ShuffleSeries.ShuffleEngine.Application.Common.Models;
using ShuffleSeries.ShuffleEngine.Domain.Models;
using ShuffleSeries.ShuffleEngine.Infrastructure.Services;
using StackExchange.Redis;

namespace ShuffleSeries.ShuffleEngine.Tests.Infrastructure.Services;

public class RedisUserShuffleStateStoreTests
{
    [Fact]
    public async Task AddToCooldownAsync_ShouldEvictExpiredAddItemAndApplyTtlInSingleBatch()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var batchMock = new Mock<IBatch>();
        batchMock.Setup(b => b.SortedSetRemoveRangeByScoreAsync(It.IsAny<RedisKey>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<Exclude>(), It.IsAny<CommandFlags>())).ReturnsAsync(0);
        batchMock.Setup(b => b.SortedSetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<double>(), It.IsAny<SortedSetWhen>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
        batchMock.Setup(b => b.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan?>(), It.IsAny<ExpireWhen>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);

        var dbMock = new Mock<IDatabase>();
        dbMock.Setup(d => d.CreateBatch(It.IsAny<object?>())).Returns(batchMock.Object);

        var redisMock = new Mock<IConnectionMultiplexer>();
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(dbMock.Object);

        var store = new RedisUserShuffleStateStore(redisMock.Object, Options.Create(new RedisOptions { InstanceName = "test:" }), TimeProvider.System);

        await store.AddToCooldownAsync(userId, itemId);

        batchMock.Verify(b => b.SortedSetRemoveRangeByScoreAsync(
            (RedisKey)$"test:cooldown:user:{userId}",
            double.NegativeInfinity,
            It.IsAny<double>(),
            Exclude.None,
            CommandFlags.None), Times.Once);
        batchMock.Verify(b => b.SortedSetAddAsync(
            (RedisKey)$"test:cooldown:user:{userId}",
            (RedisValue)itemId.ToString(),
            It.IsAny<double>(),
            SortedSetWhen.Always,
            CommandFlags.None), Times.Once);
        batchMock.Verify(b => b.KeyExpireAsync(
            (RedisKey)$"test:cooldown:user:{userId}",
            It.Is<TimeSpan?>(t => t.HasValue && t.Value >= TimeSpan.FromHours(24)),
            ExpireWhen.Always,
            CommandFlags.None), Times.Once);
        batchMock.Verify(b => b.Execute(), Times.Once);
    }

    [Fact]
    public async Task GetFeatureVectorAsync_ShouldParseAndClampMoodGenreDirectorAndOnboardingFields()
    {
        var userId = Guid.NewGuid();
        var moodId = Guid.NewGuid();
        var genreId = Guid.NewGuid();
        var onboardingMoodId = Guid.NewGuid();

        var dbMock = new Mock<IDatabase>();
        dbMock.Setup(d => d.HashGetAllAsync((RedisKey)$"test:shuffle:user:{userId}:affinity", It.IsAny<CommandFlags>()))
            .ReturnsAsync(
            [
                new HashEntry($"mood:{moodId}", "2.75"),       // exceeds +1.50 clamp
                new HashEntry($"genre:{genreId}", "-3.00"),    // below -0.80 clamp
                new HashEntry("director:christopher nolan", "0.45"),
                new HashEntry($"onboarding:mood:{onboardingMoodId}", "1"),
                new HashEntry("updatedAtUtc", "2026-11-27T10:00:00.0000000Z")
            ]);

        var redisMock = new Mock<IConnectionMultiplexer>();
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(dbMock.Object);

        var store = new RedisUserShuffleStateStore(redisMock.Object, Options.Create(new RedisOptions { InstanceName = "test:" }), TimeProvider.System);

        var vector = await store.GetFeatureVectorAsync(userId);

        vector.MoodWeights[moodId].Should().Be(1.50);
        vector.GenreWeights[genreId].Should().Be(-0.80);
        vector.DirectorWeights["christopher nolan"].Should().BeApproximately(0.45, 0.001);
        vector.OnboardingMoodIds.Should().Contain(onboardingMoodId);
    }

    [Fact]
    public async Task ApplyMediaRatingAffinityAsync_ShouldEvaluateAtomicLuaScriptWithExpectedDeltaAndFields()
    {
        var userId = Guid.NewGuid();
        var moodId = Guid.NewGuid();
        var genreId = Guid.NewGuid();
        var context = new ShuffleMediaAffinityContext(Guid.NewGuid(), [moodId], [genreId], "  Christopher Nolan ");

        var dbMock = new Mock<IDatabase>();
        dbMock.Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]?>(),
                It.IsAny<RedisValue[]?>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(1));

        var redisMock = new Mock<IConnectionMultiplexer>();
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(dbMock.Object);

        var store = new RedisUserShuffleStateStore(redisMock.Object, Options.Create(new RedisOptions { InstanceName = "test:" }), TimeProvider.System);

        await store.ApplyMediaRatingAffinityAsync(userId, context, MediaRatingTier.DoubleThumbsUp);

        dbMock.Verify(d => d.ScriptEvaluateAsync(
            It.IsAny<string>(),
            It.Is<RedisKey[]?>(k => k != null && k.Length == 1 && k[0] == (RedisKey)$"test:shuffle:user:{userId}:affinity"),
            It.Is<RedisValue[]?>(a =>
                a != null &&
                (string?)a[0] == "0.3" &&
                a.Contains((RedisValue)$"mood:{moodId}") &&
                a.Contains((RedisValue)$"genre:{genreId}") &&
                a.Contains((RedisValue)"director:christopher nolan")),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task SetEntitlementAsync_WhenLuaRejectsOutOfOrderEvent_ShouldReturnFalse()
    {
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var entitlement = new UserShuffleEntitlement(userId, true, "VIP_MONTHLY", now.AddDays(30), now.Ticks, now);

        var dbMock = new Mock<IDatabase>();
        dbMock.Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]?>(),
                It.IsAny<RedisValue[]?>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(0));

        var redisMock = new Mock<IConnectionMultiplexer>();
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(dbMock.Object);

        var store = new RedisUserShuffleStateStore(redisMock.Object, Options.Create(new RedisOptions { InstanceName = "test:" }), TimeProvider.System);

        var applied = await store.SetEntitlementAsync(entitlement);

        applied.Should().BeFalse();
    }

    [Fact]
    public async Task PurgeUserStateAsync_ShouldDeleteCooldownAffinityAndEntitlementKeys()
    {
        var userId = Guid.NewGuid();
        var dbMock = new Mock<IDatabase>();
        dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>())).ReturnsAsync(3);

        var redisMock = new Mock<IConnectionMultiplexer>();
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(dbMock.Object);

        var store = new RedisUserShuffleStateStore(redisMock.Object, Options.Create(new RedisOptions { InstanceName = "test:" }), TimeProvider.System);

        await store.PurgeUserStateAsync(userId);

        dbMock.Verify(d => d.KeyDeleteAsync(
            It.Is<RedisKey[]>(keys =>
                keys.Length == 3 &&
                keys.Contains((RedisKey)$"test:cooldown:user:{userId}") &&
                keys.Contains((RedisKey)$"test:shuffle:user:{userId}:affinity") &&
                keys.Contains((RedisKey)$"test:shuffle:user:{userId}:entitlement")),
            It.IsAny<CommandFlags>()), Times.Once);
    }
}
