namespace ShuffleSeries.Catalog.Api.Contracts.Requests.Episodes;

public sealed record UpdateEpisodeRequest(
    int EpisodeNumber,
    bool IsStandalone,
    int? RuntimeMinutes = null,
    double? ImdbRating = null,
    string? ThumbnailUrl = null);
