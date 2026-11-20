using ShuffleSeries.Catalog.Domain.Enums;

namespace ShuffleSeries.Catalog.Api.Contracts.Requests.Series;

public sealed record UpdateSeriesRequest(
    int TmdbId,
    string ImdbId,
    int ReleaseYear,
    SeriesType SeriesType,
    int TotalSeasons,
    double? ImdbRating = null,
    int? ImdbVotes = null,
    string? PosterUrl = null,
    string? BackdropUrl = null);
