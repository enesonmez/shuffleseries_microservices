using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Enums;
using SeriesEntity = ShuffleSeries.Catalog.Domain.Entities.Series;

namespace ShuffleSeries.Catalog.Application.Features.ShuffleSync.Responses;

public sealed record ShuffleMediaItemResponse(
    Guid Id,
    MediaType MediaType,
    int TmdbId,
    string ImdbId,
    double? ImdbRating,
    int? ImdbVotes,
    int ReleaseYear,
    string PosterUrl,
    string? BackdropUrl,
    int? RuntimeMinutes,
    string? Director,
    SeriesType? SeriesType,
    int? TotalSeasons,
    IReadOnlyList<Guid> PlatformIds,
    IReadOnlyList<Guid> MoodIds,
    IReadOnlyList<Guid> GenreIds,
    IReadOnlyList<ShufflePlatformLinkDto> PlatformLinks,
    IReadOnlyDictionary<string, LocalizedMediaPayloadDto> Translations)
{
    public static ShuffleMediaItemResponse FromEntity(Media media)
    {
        ArgumentNullException.ThrowIfNull(media);

        var movie = media as Movie;
        var series = media as SeriesEntity;

        var platformLinks = media.MediaPlatforms
            .Where(mp => mp.Platform is not null)
            .Select(mp => new ShufflePlatformLinkDto(
                mp.PlatformId,
                mp.Platform!.Name,
                mp.Platform!.Slug,
                mp.Platform!.LogoUrl,
                mp.WebUrl,
                mp.AppSchemeUrl))
            .ToList();

        var platformIds = media.MediaPlatforms
            .Select(mp => mp.PlatformId)
            .Distinct()
            .ToList();

        var moodIds = media.MediaMoods
            .Select(mm => mm.MoodId)
            .Distinct()
            .ToList();

        var genreIds = media.MediaGenres
            .Select(mg => mg.GenreId)
            .Distinct()
            .ToList();

        var translations = media.Translations
            .ToDictionary(
                t => t.LanguageCode.Value,
                t => new LocalizedMediaPayloadDto(t.Title, t.Overview, t.Tagline),
                StringComparer.OrdinalIgnoreCase);

        return new ShuffleMediaItemResponse(
            media.Id,
            media.MediaType,
            media.TmdbId,
            media.ImdbId,
            media.ImdbRating,
            media.ImdbVotes,
            media.ReleaseYear,
            media.PosterUrl,
            media.BackdropUrl,
            movie?.RuntimeMinutes,
            movie?.Director,
            series?.SeriesType,
            series?.TotalSeasons,
            platformIds,
            moodIds,
            genreIds,
            platformLinks,
            translations);
    }
}
