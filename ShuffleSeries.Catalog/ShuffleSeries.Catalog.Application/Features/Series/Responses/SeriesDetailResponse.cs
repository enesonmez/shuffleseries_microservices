using ShuffleSeries.Catalog.Domain.Enums;

namespace ShuffleSeries.Catalog.Application.Features.Series.Responses;

public sealed record SeriesDetailResponse(
    Guid Id,
    int TmdbId,
    string ImdbId,
    int ReleaseYear,
    SeriesType SeriesType,
    int TotalSeasons,
    string PosterUrl,
    string? BackdropUrl,
    double? ImdbRating,
    int? ImdbVotes,
    IReadOnlyList<SeriesTranslationDto> Translations,
    string Title = "",
    string Overview = "",
    string? Tagline = null,
    string ResolvedLanguage = "tr",
    IReadOnlyList<SeriesSeasonDto>? Seasons = null,
    IReadOnlyList<SeriesPlatformDto>? Platforms = null,
    IReadOnlyList<SeriesMoodDto>? Moods = null,
    IReadOnlyList<SeriesGenreDto>? Genres = null);

public sealed record SeriesTranslationDto(
    string LanguageCode,
    string Title,
    string Overview,
    string? Tagline);

public sealed record SeriesSeasonDto(
    Guid SeasonId,
    int SeasonNumber,
    string? Title,
    IReadOnlyList<SeriesEpisodeDto> Episodes);

public sealed record SeriesEpisodeDto(
    Guid EpisodeId,
    int EpisodeNumber,
    bool IsStandalone,
    int? RuntimeMinutes,
    double? ImdbRating,
    string? ThumbnailUrl,
    string Title,
    string? Overview);

public sealed record SeriesPlatformDto(
    Guid PlatformId,
    string Name,
    string Slug,
    string LogoUrl,
    string WebUrl,
    string? AppSchemeUrl);

public sealed record SeriesMoodDto(
    Guid MoodId,
    string Code,
    string Name,
    string AmbientColorHex,
    string? IconUrl);

public sealed record SeriesGenreDto(
    Guid GenreId,
    string Code,
    string Name);
