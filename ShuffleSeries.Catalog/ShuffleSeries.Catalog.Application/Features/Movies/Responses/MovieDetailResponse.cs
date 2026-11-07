namespace ShuffleSeries.Catalog.Application.Features.Movies.Responses;

public sealed record MovieDetailResponse(
    Guid Id,
    int TmdbId,
    string ImdbId,
    string Title,
    string Overview,
    string? Tagline,
    string ResolvedLanguage,
    int ReleaseYear,
    int RuntimeMinutes,
    string? Director,
    string PosterUrl,
    string? BackdropUrl,
    double? ImdbRating,
    int? ImdbVotes,
    IReadOnlyList<MoviePlatformDto> Platforms,
    IReadOnlyList<MovieMoodDto> Moods,
    IReadOnlyList<MovieGenreDto> Genres,
    IReadOnlyList<MovieTranslationDto> Translations);
