using System.Collections.Concurrent;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Application.Common.Models;
using ShuffleSeries.ShuffleEngine.Domain.Enums;
using ShuffleSeries.ShuffleEngine.Domain.Models;
using ShuffleSeries.ShuffleEngine.Domain.Services;

namespace ShuffleSeries.ShuffleEngine.IntegrationTests.Infrastructure;

public sealed class FakeCatalogSyncGrpcClient : ICatalogSyncGrpcClient
{
    public static readonly Guid SeedPlatformNetflixId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid SeedPlatformPrimeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid SeedMoodMindBendingId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid SeedGenreSciFiId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid SeedMovieInceptionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid SeedMovieLowRatedId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid SeedSeriesBlackMirrorId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    public static readonly Guid SeedEpisodeSanJuniperoId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    private readonly ConcurrentDictionary<Guid, ShuffleMediaSnapshot> _media = new();
    private readonly ConcurrentDictionary<Guid, ShuffleEpisodeSnapshot> _episodes = new();

    public FakeCatalogSyncGrpcClient()
    {
        ResetToSeedCatalog();
    }

    public void ResetToSeedCatalog()
    {
        _media.Clear();
        _episodes.Clear();

        var netflixLink = new ShufflePlatformDeepLink(
            SeedPlatformNetflixId, "Netflix", "netflix", "https://img/netflix.svg", "https://netflix.com/title/1", "netflix://title/1");

        UpsertMedia(new ShuffleMediaSnapshot(
            SeedMovieInceptionId,
            ShuffleMediaKind.Movie,
            27205,
            "tt1375666",
            8.8,
            2_500_000,
            2010,
            "https://img/inception.jpg",
            null,
            148,
            "Christopher Nolan",
            null,
            null,
            BaseQualityScoreCalculator.CalculateMediaScore(8.8, 2_500_000, 2010, 2026),
            IsSafetyNetEligible: true,
            [SeedPlatformNetflixId, SeedPlatformPrimeId],
            [SeedMoodMindBendingId],
            [SeedGenreSciFiId],
            [netflixLink],
            new Dictionary<string, ShuffleLocalizedText>
            {
                ["tr"] = new("Başlangıç", "Rüya içinde rüya"),
                ["en"] = new("Inception", "Dream heist")
            }));

        UpsertMedia(new ShuffleMediaSnapshot(
            SeedMovieLowRatedId,
            ShuffleMediaKind.Movie,
            99991,
            "tt9999991",
            6.1,
            12_000,
            2021,
            "https://img/bmovie.jpg",
            null,
            95,
            "Indie Director",
            null,
            null,
            BaseQualityScoreCalculator.CalculateMediaScore(6.1, 12_000, 2021, 2026),
            IsSafetyNetEligible: false,
            [SeedPlatformNetflixId],
            [SeedMoodMindBendingId],
            [SeedGenreSciFiId],
            [netflixLink],
            new Dictionary<string, ShuffleLocalizedText>
            {
                ["en"] = new("Average Sci-Fi", "Below safety net threshold")
            }));

        UpsertMedia(new ShuffleMediaSnapshot(
            SeedSeriesBlackMirrorId,
            ShuffleMediaKind.Series,
            42009,
            "tt2085059",
            8.7,
            620_000,
            2011,
            "https://img/bm.jpg",
            null,
            null,
            null,
            2,
            6,
            BaseQualityScoreCalculator.CalculateMediaScore(8.7, 620_000, 2011, 2026),
            IsSafetyNetEligible: true,
            [SeedPlatformNetflixId],
            [SeedMoodMindBendingId],
            [SeedGenreSciFiId],
            [netflixLink],
            new Dictionary<string, ShuffleLocalizedText>
            {
                ["tr"] = new("Black Mirror", "Antoloji dizisi"),
                ["en"] = new("Black Mirror", "Tech anthology")
            }));

        UpsertEpisode(new ShuffleEpisodeSnapshot(
            SeedEpisodeSanJuniperoId,
            SeedSeriesBlackMirrorId,
            Guid.NewGuid(),
            3,
            4,
            61,
            8.6,
            "https://img/sj.jpg",
            "https://img/bm.jpg",
            2016,
            BaseQualityScoreCalculator.CalculateEpisodeScore(8.6, 2016, 2026),
            IsSafetyNetEligible: true,
            [SeedPlatformNetflixId],
            [SeedMoodMindBendingId],
            [SeedGenreSciFiId],
            [netflixLink],
            new Dictionary<string, ShuffleLocalizedText> { ["en"] = new("San Junipero", "1987 nostalgia") },
            new Dictionary<string, ShuffleLocalizedText> { ["en"] = new("Black Mirror", "Tech anthology") }));
    }

    public void UpsertMedia(ShuffleMediaSnapshot media) => _media[media.Id] = media;

    public void RemoveMedia(Guid mediaId) => _media.TryRemove(mediaId, out _);

    public void UpsertEpisode(ShuffleEpisodeSnapshot episode) => _episodes[episode.EpisodeId] = episode;

    public void RemoveEpisode(Guid episodeId) => _episodes.TryRemove(episodeId, out _);

    public Task<ShuffleMediaSnapshot?> GetMediaByIdAsync(Guid mediaId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_media.GetValueOrDefault(mediaId));

    public Task<ShuffleEpisodeSnapshot?> GetStandaloneEpisodeByIdAsync(Guid episodeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_episodes.GetValueOrDefault(episodeId));

    public Task<IReadOnlyList<ShuffleEpisodeSnapshot>> GetStandaloneEpisodesBySeriesIdAsync(
        Guid seriesId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ShuffleEpisodeSnapshot> list = _episodes.Values
            .Where(e => e.SeriesId == seriesId)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<CatalogPagedResult<ShuffleMediaSnapshot>> GetBulkMediaPageAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var all = _media.Values.OrderBy(x => x.Id).ToList();
        var items = all.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new CatalogPagedResult<ShuffleMediaSnapshot>(items, all.Count, pageNumber, pageSize));
    }

    public Task<CatalogPagedResult<ShuffleEpisodeSnapshot>> GetStandaloneEpisodesPageAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var all = _episodes.Values.OrderBy(x => x.EpisodeId).ToList();
        var items = all.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new CatalogPagedResult<ShuffleEpisodeSnapshot>(items, all.Count, pageNumber, pageSize));
    }
}
