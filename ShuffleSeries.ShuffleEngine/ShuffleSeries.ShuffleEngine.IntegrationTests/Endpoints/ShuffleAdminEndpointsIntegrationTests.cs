using System.Net;
using System.Net.Http.Json;
using ShuffleSeries.ShuffleEngine.Api.Contracts.Requests.CacheSync;
using ShuffleSeries.ShuffleEngine.Application.Features.CacheSync.Responses;
using ShuffleSeries.ShuffleEngine.IntegrationTests.Infrastructure;

namespace ShuffleSeries.ShuffleEngine.IntegrationTests.Endpoints;

public class ShuffleAdminEndpointsIntegrationTests(ShuffleEngineApiFactory factory)
    : ShuffleEngineIntegrationTestBase(factory)
{
    [Fact]
    public async Task AdminEndpoints_ShouldEnforceRbac_401ForAnonymous_And_403ForStandardUser()
    {
        using var anonClient = CreateUnauthorizedClient();
        var anonResp = await anonClient.GetAsync("/api/shuffle/admin/pool-status");
        anonResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var standardClient = CreateAuthorizedClient(role: SystemRoles.Standard);
        var forbiddenResp = await standardClient.GetAsync("/api/shuffle/admin/pool-status");
        forbiddenResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var forbiddenWarm = await standardClient.PostAsJsonAsync(
            "/api/shuffle/admin/warm-cache",
            new WarmShuffleCacheRequest(false, "attempt"));
        forbiddenWarm.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task WarmCache_And_PoolStatus_ShouldPopulateRedisPoolsSafetyNetAndDeterministicTtls()
    {
        // 1. Cold state before warming
        var coldStatus = await Client.GetFromJsonAsync<ShufflePoolStatusResponse>("/api/shuffle/admin/pool-status");
        coldStatus.Should().NotBeNull();
        coldStatus!.IsWarmed.Should().BeFalse();
        coldStatus.MoviesCount.Should().Be(0);

        // 2. Trigger Cold-Start Cache Warming
        var warmHttpResp = await Client.PostAsJsonAsync(
            "/api/shuffle/admin/warm-cache",
            new WarmShuffleCacheRequest(ForceRebuild: false, TriggerReason: "Cold start integration test"));
        warmHttpResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var warmResult = await warmHttpResp.Content.ReadFromJsonAsync<WarmShuffleCacheResponse>();
        warmResult.Should().NotBeNull();
        warmResult!.WasSkipped.Should().BeFalse();
        warmResult.MoviesSynced.Should().Be(2);              // Inception (8.8) + Average Sci-Fi (6.1)
        warmResult.SeriesSynced.Should().Be(1);              // Black Mirror (8.7)
        warmResult.StandaloneEpisodesSynced.Should().Be(1);  // San Junipero (8.6)
        warmResult.SafetyNetItemsCount.Should().Be(3);       // Only IMDb >= 7.5 items!

        // 3. Second call without ForceRebuild skips immediately
        var secondWarmResp = await Client.PostAsJsonAsync(
            "/api/shuffle/admin/warm-cache",
            new WarmShuffleCacheRequest(ForceRebuild: false));
        var secondWarmResult = await secondWarmResp.Content.ReadFromJsonAsync<WarmShuffleCacheResponse>();
        secondWarmResult!.WasSkipped.Should().BeTrue();

        // 4. Verify Pool Status & Safety Net Exclusion of 6.1-rated Movie
        var hotStatus = await Client.GetFromJsonAsync<ShufflePoolStatusResponse>("/api/shuffle/admin/pool-status");
        hotStatus!.IsWarmed.Should().BeTrue();
        hotStatus.MoviesCount.Should().Be(2);
        hotStatus.SafetyNetMoviesCount.Should().Be(1);
        hotStatus.SeriesCount.Should().Be(1);
        hotStatus.SafetyNetSeriesCount.Should().Be(1);
        hotStatus.StandaloneEpisodesCount.Should().Be(1);
        hotStatus.SafetyNetEpisodesCount.Should().Be(1);

        // 5. Verify Redis Key Topology & Deterministic TTL Mandate (Performance Guard #6)
        var db = Factory.Redis.GetDatabase();
        var inceptionKey = $"shuffleseries:shuffle:media:{FakeCatalogSyncGrpcClient.SeedMovieInceptionId}";
        var moviePoolKey = "shuffleseries:shuffle:pool:movies";
        var safetyMoviesKey = "shuffleseries:shuffle:safety-net:movies";
        var moodSetKey = $"shuffleseries:shuffle:idx:mood:{FakeCatalogSyncGrpcClient.SeedMoodMindBendingId}:movies";

        (await db.KeyTimeToLiveAsync(inceptionKey)).Should().NotBeNull().And.BeGreaterThan(TimeSpan.FromHours(23));
        (await db.KeyTimeToLiveAsync(moviePoolKey)).Should().NotBeNull().And.BeGreaterThan(TimeSpan.FromHours(23));
        (await db.KeyTimeToLiveAsync(safetyMoviesKey)).Should().NotBeNull().And.BeGreaterThan(TimeSpan.FromHours(23));
        (await db.KeyTimeToLiveAsync(moodSetKey)).Should().NotBeNull().And.BeGreaterThan(TimeSpan.FromHours(23));

        (await db.SortedSetScoreAsync(safetyMoviesKey, FakeCatalogSyncGrpcClient.SeedMovieInceptionId.ToString())).Should().NotBeNull();
        (await db.SortedSetScoreAsync(safetyMoviesKey, FakeCatalogSyncGrpcClient.SeedMovieLowRatedId.ToString())).Should().BeNull();
    }

    [Fact]
    public async Task WarmCache_WhenForceRebuild_ShouldPruneOfflineDeletedGhostMediaAndEpisodes()
    {
        // 1. Initial warm with full seed catalog (2 movies, 1 series, 1 episode)
        var initialResp = await Client.PostAsJsonAsync(
            "/api/shuffle/admin/warm-cache",
            new WarmShuffleCacheRequest(ForceRebuild: false, TriggerReason: "Initial warm"));
        initialResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Simulate offline deletion in Catalog (no RabbitMQ delete event delivered)
        Factory.FakeCatalogClient.RemoveMedia(FakeCatalogSyncGrpcClient.SeedMovieLowRatedId);
        Factory.FakeCatalogClient.RemoveEpisode(FakeCatalogSyncGrpcClient.SeedEpisodeSanJuniperoId);

        // 3. Trigger ForceRebuild reconciliation
        var rebuildResp = await Client.PostAsJsonAsync(
            "/api/shuffle/admin/warm-cache",
            new WarmShuffleCacheRequest(ForceRebuild: true, TriggerReason: "Reconcile ghost entries"));
        rebuildResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var rebuildResult = await rebuildResp.Content.ReadFromJsonAsync<WarmShuffleCacheResponse>();
        rebuildResult!.WasSkipped.Should().BeFalse();
        rebuildResult.MoviesSynced.Should().Be(1);
        rebuildResult.StandaloneEpisodesSynced.Should().Be(0);

        // 4. Verify ghost movie and ghost episode were evicted from Redis pools & indexes
        var status = await Client.GetFromJsonAsync<ShufflePoolStatusResponse>("/api/shuffle/admin/pool-status");
        status!.MoviesCount.Should().Be(1);
        status.StandaloneEpisodesCount.Should().Be(0);
        status.SafetyNetEpisodesCount.Should().Be(0);

        var db = Factory.Redis.GetDatabase();
        (await db.KeyExistsAsync($"shuffleseries:shuffle:media:{FakeCatalogSyncGrpcClient.SeedMovieLowRatedId}")).Should().BeFalse();
        (await db.KeyExistsAsync($"shuffleseries:shuffle:episode:{FakeCatalogSyncGrpcClient.SeedEpisodeSanJuniperoId}")).Should().BeFalse();
    }

    [Fact]
    public async Task WarmCache_WhenTriggerReasonInvalid_ShouldReturn400ProblemDetails()
    {
        var badResp = await Client.PostAsJsonAsync(
            "/api/shuffle/admin/warm-cache",
            new WarmShuffleCacheRequest(false, new string('X', 120)));

        badResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
