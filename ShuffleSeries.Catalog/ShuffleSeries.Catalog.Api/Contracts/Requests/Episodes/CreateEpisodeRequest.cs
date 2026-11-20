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
