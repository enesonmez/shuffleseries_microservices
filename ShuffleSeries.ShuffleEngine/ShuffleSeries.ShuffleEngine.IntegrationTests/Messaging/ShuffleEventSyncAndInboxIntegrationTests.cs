using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Shared.Core.Domain.Enums;
using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Domain.Enums;
using ShuffleSeries.ShuffleEngine.Domain.Models;
using ShuffleSeries.ShuffleEngine.Domain.Services;
using ShuffleSeries.ShuffleEngine.Infrastructure.Persistence;
using ShuffleSeries.ShuffleEngine.IntegrationTests.Infrastructure;
using Wolverine;

namespace ShuffleSeries.ShuffleEngine.IntegrationTests.Messaging;

public class ShuffleEventSyncAndInboxIntegrationTests(ShuffleEngineApiFactory factory)
    : ShuffleEngineIntegrationTestBase(factory)
{
    [Fact]
    public async Task MediaAndEpisodeLifecycleEvents_ShouldSyncAndCleanRedisAtomically()
    {
        using var scope = Factory.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var poolStore = scope.ServiceProvider.GetRequiredService<IShufflePoolStore>();

        // 1. MediaCreatedEvent (Series + child standalone episode)
        await bus.InvokeAsync(new MediaCreatedEvent(
            Guid.NewGuid(),
            FakeCatalogSyncGrpcClient.SeedSeriesBlackMirrorId,
            "Series",
            DateTime.UtcNow));

        var seriesInRedis = await poolStore.GetMediaByIdAsync(FakeCatalogSyncGrpcClient.SeedSeriesBlackMirrorId);
        seriesInRedis.Should().NotBeNull();
        var episodeInRedis = await poolStore.GetEpisodeByIdAsync(FakeCatalogSyncGrpcClient.SeedEpisodeSanJuniperoId);
        episodeInRedis.Should().NotBeNull();

        // 2. MediaUpdatedEvent: Update Inception rating below 7.5 and remove Prime Video platform
        var updatedInception = new ShuffleMediaSnapshot(
            FakeCatalogSyncGrpcClient.SeedMovieInceptionId,
            ShuffleMediaKind.Movie,
            27205,
            "tt1375666",
            7.1, // Dropped below 7.5 Safety Net threshold!
            2_500_000,
            2010,
            "https://img/inception.jpg",
            null,
            148,
            "Christopher Nolan",
            null,
            null,
            BaseQualityScoreCalculator.CalculateMediaScore(7.1, 2_500_000, 2010, 2026),
            IsSafetyNetEligible: false,
            [FakeCatalogSyncGrpcClient.SeedPlatformNetflixId], // Removed SeedPlatformPrimeId
            [FakeCatalogSyncGrpcClient.SeedMoodMindBendingId],
            [FakeCatalogSyncGrpcClient.SeedGenreSciFiId],
            [],
            new Dictionary<string, ShuffleLocalizedText> { ["en"] = new("Inception Edited", "Updated") });

        // First insert original Inception (with Prime Video & Safety Net = true)
        await bus.InvokeAsync(new MediaCreatedEvent(
            Guid.NewGuid(),
            FakeCatalogSyncGrpcClient.SeedMovieInceptionId,
            "Movie",
            DateTime.UtcNow.AddMinutes(-5)));

        Factory.FakeCatalogClient.UpsertMedia(updatedInception);

        await bus.InvokeAsync(new MediaUpdatedEvent(
            Guid.NewGuid(),
            FakeCatalogSyncGrpcClient.SeedMovieInceptionId,
            "Movie",
            DateTime.UtcNow));

        var db = Factory.Redis.GetDatabase();
        var primeSetKey = $"shuffleseries:shuffle:idx:platform:{FakeCatalogSyncGrpcClient.SeedPlatformPrimeId}:movies";
        var safetyMoviesKey = "shuffleseries:shuffle:safety-net:movies";

        // Prime Video stale index membership and Safety Net membership MUST be atomically removed!
        (await db.SetContainsAsync(primeSetKey, FakeCatalogSyncGrpcClient.SeedMovieInceptionId.ToString())).Should().BeFalse();
        (await db.SortedSetScoreAsync(safetyMoviesKey, FakeCatalogSyncGrpcClient.SeedMovieInceptionId.ToString())).Should().BeNull();

        // 3. MediaDeletedEvent on Series MUST cascade-remove child standalone episodes (Zero Ghost Active Episodes!)
        await bus.InvokeAsync(new MediaDeletedEvent(
            Guid.NewGuid(),
            FakeCatalogSyncGrpcClient.SeedSeriesBlackMirrorId,
            "Series",
            DateTime.UtcNow.AddSeconds(1)));

        (await poolStore.GetMediaByIdAsync(FakeCatalogSyncGrpcClient.SeedSeriesBlackMirrorId)).Should().BeNull();
        (await poolStore.GetEpisodeByIdAsync(FakeCatalogSyncGrpcClient.SeedEpisodeSanJuniperoId)).Should().BeNull();
    }

    [Fact]
    public async Task MediaUpdatedEvent_WhenSeriesLosesStandaloneEpisode_ShouldEvictOrphanEpisodeFromRedis()
    {
        using var scope = Factory.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var poolStore = scope.ServiceProvider.GetRequiredService<IShufflePoolStore>();

        await bus.InvokeAsync(new MediaCreatedEvent(
            Guid.NewGuid(),
            FakeCatalogSyncGrpcClient.SeedSeriesBlackMirrorId,
            "Series",
            DateTime.UtcNow.AddMinutes(-2)));

        (await poolStore.GetEpisodeByIdAsync(FakeCatalogSyncGrpcClient.SeedEpisodeSanJuniperoId)).Should().NotBeNull();

        // Remove episode from Catalog without delivering EpisodeDeletedEvent, then emit MediaUpdatedEvent on Series
        Factory.FakeCatalogClient.RemoveEpisode(FakeCatalogSyncGrpcClient.SeedEpisodeSanJuniperoId);

        await bus.InvokeAsync(new MediaUpdatedEvent(
            Guid.NewGuid(),
            FakeCatalogSyncGrpcClient.SeedSeriesBlackMirrorId,
            "Series",
            DateTime.UtcNow));

        (await poolStore.GetEpisodeByIdAsync(FakeCatalogSyncGrpcClient.SeedEpisodeSanJuniperoId)).Should().BeNull();
    }

    [Fact]
    public async Task RelationalInbox_WhenHandlerFailsThenRetries_ShouldRecordErrorAndReclaimOnRetry()
    {
        using var scope = Factory.Services.CreateScope();
        var inboxService = scope.ServiceProvider.GetRequiredService<IInboxIdempotencyService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShuffleEngineDbContext>();

        var eventId = Guid.NewGuid();
        var evt = new EpisodeDeletedEvent(eventId, Guid.NewGuid(), DateTime.UtcNow, "corr-retry-pg");

        var failAct = async () => await inboxService.ExecuteIdempotentAsync(
            evt,
            _ => throw new InvalidOperationException("Simulated gRPC outage"),
            CancellationToken.None);

        await failAct.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Simulated gRPC outage*");

        var failedRow = await dbContext.InboxMessages.AsNoTracking().SingleAsync(x => x.Id == eventId);
        failedRow.IsProcessed.Should().BeFalse();
        failedRow.Error.Should().Contain("Simulated gRPC outage");

        var executions = 0;
        var reclaimed = await inboxService.ExecuteIdempotentAsync(
            evt,
            _ =>
            {
                executions++;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        var duplicateSkipped = await inboxService.ExecuteIdempotentAsync(
            evt,
            _ =>
            {
                executions++;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        reclaimed.Should().BeTrue();
        duplicateSkipped.Should().BeFalse();
        executions.Should().Be(1);

        var sealedRow = await dbContext.InboxMessages.AsNoTracking().SingleAsync(x => x.Id == eventId);
        sealedRow.IsProcessed.Should().BeTrue();
        sealedRow.Error.Should().BeNull();
    }

    [Fact]
    public async Task InboxIdempotency_WhenSameMediaRatedEventDeliveredTwice_ShouldApplyAffinityOnlyOnce()
    {
        using var scope = Factory.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var stateStore = scope.ServiceProvider.GetRequiredService<IUserShuffleStateStore>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShuffleEngineDbContext>();

        var userId = Guid.NewGuid();
        var duplicateEventId = Guid.NewGuid();
        var ratedEvent = new MediaRatedEvent(
            duplicateEventId,
            userId,
            FakeCatalogSyncGrpcClient.SeedMovieInceptionId,
            MediaRatingTier.DoubleThumbsUp,
            DateTime.UtcNow,
            "corr-idempotent-test");

        // Deliver #1
        await bus.InvokeAsync(ratedEvent);

        // Deliver #2 (Simulated RabbitMQ redelivery with identical Event.Id)
        await bus.InvokeAsync(ratedEvent);

        var vector = await stateStore.GetFeatureVectorAsync(userId);

        // Must be +0.30 (applied ONCE), NOT +0.60!
        vector.MoodWeights[FakeCatalogSyncGrpcClient.SeedMoodMindBendingId].Should().BeApproximately(0.30, 0.001);
        vector.GenreWeights[FakeCatalogSyncGrpcClient.SeedGenreSciFiId].Should().BeApproximately(0.30, 0.001);
        vector.DirectorWeights["christopher nolan"].Should().BeApproximately(0.30, 0.001);

        var inboxRow = await dbContext.InboxMessages.SingleAsync(x => x.Id == duplicateEventId);
        inboxRow.IsProcessed.Should().BeTrue();
        inboxRow.CorrelationId.Should().Be("corr-idempotent-test");
    }

    [Fact]
    public async Task SubscriptionEntitlement_Cooldown24h_And_AccountDeletionPurge_ShouldWorkEndToEnd()
    {
        using var scope = Factory.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var stateStore = scope.ServiceProvider.GetRequiredService<IUserShuffleStateStore>();

        var userId = Guid.NewGuid();
        var t1 = new DateTime(2026, 11, 27, 10, 0, 0, DateTimeKind.Utc);
        var t2 = t1.AddMinutes(5);

        // 1. User subscribes at T1
        await bus.InvokeAsync(new UserSubscribedEvent(
            Guid.NewGuid(),
            userId,
            "VIP_ANNUAL",
            DateTime.UtcNow.AddDays(365),
            t1));

        var subState = await stateStore.GetEntitlementAsync(userId);
        subState.Should().NotBeNull();
        subState!.IsCurrentlyPremium(DateTime.UtcNow).Should().BeTrue();

        // 2. User cancels at T2 (T2 > T1)
        await bus.InvokeAsync(new UserSubscriptionCancelledEvent(
            Guid.NewGuid(),
            userId,
            t2,
            t2));

        var cancelledState = await stateStore.GetEntitlementAsync(userId);
        cancelledState!.IsCurrentlyPremium(DateTime.UtcNow).Should().BeFalse();

        // 3. Out-of-order delayed event from T0 (< T2) arrives late -> MUST NOT overwrite T2 cancellation!
        var t0 = t1.AddMinutes(-10);
        await bus.InvokeAsync(new UserSubscribedEvent(
            Guid.NewGuid(),
            userId,
            "STALE_PLAN",
            DateTime.UtcNow.AddDays(30),
            t0));

        var afterStale = await stateStore.GetEntitlementAsync(userId);
        afterStale!.IsPremium.Should().BeFalse();

        // 4. 24-Hour Cooldown Set (`cooldown:user:{userId}`)
        var skippedMovieId = Guid.NewGuid();
        var expiredMovieId = Guid.NewGuid();
        await stateStore.AddToCooldownAsync(userId, skippedMovieId);

        // Manually inject an already-expired entry into the same ZSET to verify per-item 24h filtering
        var db = Factory.Redis.GetDatabase();
        var pastUnix = DateTimeOffset.UtcNow.AddHours(-25).ToUnixTimeSeconds();
        await db.SortedSetAddAsync($"shuffleseries:cooldown:user:{userId}", expiredMovieId.ToString(), pastUnix);

        var activeCooldowns = await stateStore.GetActiveCooldownIdsAsync(userId);
        activeCooldowns.Should().Contain(skippedMovieId);
        activeCooldowns.Should().NotContain(expiredMovieId);

        // 5. Onboarding preferences + GDPR UserAccountDeletedEvent cleanup
        await stateStore.ApplyOnboardingPreferencesAsync(userId, [FakeCatalogSyncGrpcClient.SeedMoodMindBendingId]);
        await bus.InvokeAsync(new UserAccountDeletedEvent(Guid.NewGuid(), userId, "user@example.com", DateTime.UtcNow));

        (await stateStore.GetEntitlementAsync(userId)).Should().BeNull();
        (await stateStore.GetActiveCooldownIdsAsync(userId)).Should().BeEmpty();
        (await stateStore.GetFeatureVectorAsync(userId)).OnboardingMoodIds.Should().BeEmpty();
    }
}
