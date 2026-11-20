namespace ShuffleSeries.Catalog.Application.Features.Series.Responses;

public sealed record SeriesEpisodeDto(
    Guid EpisodeId,
    int EpisodeNumber,
    bool IsStandalone,
    int? RuntimeMinutes,
    double? ImdbRating,
    string? ThumbnailUrl,
    string Title,
    string? Overview);
