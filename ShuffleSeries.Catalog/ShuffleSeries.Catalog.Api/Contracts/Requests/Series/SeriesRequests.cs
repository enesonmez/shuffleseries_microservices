using ShuffleSeries.Catalog.Domain.Enums;

namespace ShuffleSeries.Catalog.Api.Contracts.Requests.Series;

public sealed record CreateSeriesRequest(
    int TmdbId,
    string ImdbId,
    int ReleaseYear,
    SeriesType SeriesType,
    string PosterUrl,
    string? BackdropUrl,
    string LanguageCode,
    string Title,
    string Overview,
    string? Tagline = null,
    double? ImdbRating = null,
    int? ImdbVotes = null,
    int TotalSeasons = 0,
    IReadOnlyList<Guid>? PlatformIds = null,
    IReadOnlyList<Guid>? MoodIds = null,
    IReadOnlyList<Guid>? GenreIds = null);

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
