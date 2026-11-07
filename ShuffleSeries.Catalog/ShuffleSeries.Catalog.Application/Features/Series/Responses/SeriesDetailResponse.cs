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
