namespace ShuffleSeries.Catalog.Application.Features.Episodes.Responses;

public sealed record EpisodeDetailResponse(
    Guid Id,
    Guid SeasonId,
    int EpisodeNumber,
    bool IsStandalone,
    int? RuntimeMinutes,
    double? ImdbRating,
    string? ThumbnailUrl,
    string Title,
    string? Overview,
    string ResolvedLanguage,
    IReadOnlyList<EpisodePlatformDto> Platforms,
    IReadOnlyList<EpisodeTranslationDto> Translations);

public sealed record EpisodePlatformDto(
    Guid PlatformId,
    string Name,
    string Slug,
    string LogoUrl,
    string WebUrl,
    string? AppSchemeUrl);

public sealed record EpisodeTranslationDto(
    string LanguageCode,
    string Title,
    string? Overview);
