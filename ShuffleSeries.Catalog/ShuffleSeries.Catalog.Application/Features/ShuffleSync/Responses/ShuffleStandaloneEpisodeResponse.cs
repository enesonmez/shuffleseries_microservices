using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.ShuffleSync.Responses;

public sealed record ShuffleStandaloneEpisodeResponse(
    Guid EpisodeId,
    Guid SeriesId,
    Guid SeasonId,
    int SeasonNumber,
    int EpisodeNumber,
    bool IsStandalone,
    int? RuntimeMinutes,
    double? ImdbRating,
    string? ThumbnailUrl,
    string SeriesPosterUrl,
    int ReleaseYear,
    IReadOnlyList<Guid> PlatformIds,
    IReadOnlyList<Guid> MoodIds,
    IReadOnlyList<Guid> GenreIds,
    IReadOnlyList<ShufflePlatformLinkDto> PlatformLinks,
    IReadOnlyDictionary<string, LocalizedEpisodePayloadDto> Translations,
    IReadOnlyDictionary<string, LocalizedMediaPayloadDto> SeriesTranslations)
{
    public static ShuffleStandaloneEpisodeResponse FromContext(StandaloneEpisodeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var (episode, season, series) = context;

        var (platformIds, platformLinks) = episode.EpisodePlatforms.Count > 0
            ? MapPlatforms(episode.EpisodePlatforms, ep => (ep.PlatformId, ep.Platform, ep.WebUrl, ep.AppSchemeUrl))
            : MapPlatforms(series.MediaPlatforms, mp => (mp.PlatformId, mp.Platform, mp.WebUrl, mp.AppSchemeUrl));

        var moodIds = series.MediaMoods
            .Select(mm => mm.MoodId)
            .Distinct()
            .ToList();

        var genreIds = series.MediaGenres
            .Select(mg => mg.GenreId)
            .Distinct()
            .ToList();

        var translations = episode.Translations
            .ToDictionary(
                t => t.LanguageCode.Value,
                t => new LocalizedEpisodePayloadDto(t.Title, t.Overview),
                StringComparer.OrdinalIgnoreCase);

        var seriesTranslations = series.Translations
            .ToDictionary(
                t => t.LanguageCode.Value,
                t => new LocalizedMediaPayloadDto(t.Title, t.Overview, t.Tagline),
                StringComparer.OrdinalIgnoreCase);

        return new ShuffleStandaloneEpisodeResponse(
            episode.Id,
            series.Id,
            season.Id,
            season.SeasonNumber,
            episode.EpisodeNumber,
            episode.IsStandalone,
            episode.RuntimeMinutes,
            episode.ImdbRating,
            episode.ThumbnailUrl,
            series.PosterUrl,
            series.ReleaseYear,
            platformIds,
            moodIds,
            genreIds,
            platformLinks,
            translations,
            seriesTranslations);
    }

    private static (IReadOnlyList<Guid> PlatformIds, IReadOnlyList<ShufflePlatformLinkDto> Links) MapPlatforms<T>(
        IEnumerable<T> source,
        Func<T, (Guid PlatformId, Platform? Platform, string WebUrl, string? AppSchemeUrl)> selector)
    {
        var entries = source.Select(selector).ToList();

        var ids = entries
            .Select(e => e.PlatformId)
            .Distinct()
            .ToList();

        var links = entries
            .Where(e => e.Platform is not null)
            .Select(e => new ShufflePlatformLinkDto(
                e.PlatformId,
                e.Platform!.Name,
                e.Platform!.Slug,
                e.Platform!.LogoUrl,
                e.WebUrl,
                e.AppSchemeUrl))
            .ToList();

        return (ids, links);
    }
}
