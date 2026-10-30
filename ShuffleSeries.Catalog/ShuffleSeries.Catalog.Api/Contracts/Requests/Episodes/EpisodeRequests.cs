namespace ShuffleSeries.Catalog.Api.Contracts.Requests.Episodes;

public sealed record CreateEpisodeRequest(
    Guid SeriesId,
    int SeasonNumber,
    int EpisodeNumber,
    string Title,
    string? Overview,
    string LanguageCode,
    bool? IsStandalone = null,
    int? RuntimeMinutes = null,
    double? ImdbRating = null,
    string? ThumbnailUrl = null);

public sealed record UpdateEpisodeRequest(
    int EpisodeNumber,
    bool IsStandalone,
    int? RuntimeMinutes = null,
    double? ImdbRating = null,
    string? ThumbnailUrl = null);

public sealed record AddEpisodePlatformRequest(
    Guid PlatformId,
    string WebUrl,
    string? AppSchemeUrl = null);

public sealed record AddOrUpdateEpisodeTranslationRequest(
    string LanguageCode,
    string Title,
    string? Overview = null);
