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
