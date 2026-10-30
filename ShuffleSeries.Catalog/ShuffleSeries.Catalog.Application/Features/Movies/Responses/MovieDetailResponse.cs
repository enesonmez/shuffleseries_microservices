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

public sealed record MoviePlatformDto(
    Guid PlatformId,
    string Name,
    string Slug,
    string LogoUrl,
    string WebUrl,
    string? AppSchemeUrl);

public sealed record MovieMoodDto(
    Guid MoodId,
    string Code,
    string Name,
    string AmbientColorHex,
    string? IconUrl);

public sealed record MovieGenreDto(
    Guid GenreId,
    string Code,
    string Name);

public sealed record MovieTranslationDto(
    string LanguageCode,
    string Title,
    string Overview,
    string? Tagline);
