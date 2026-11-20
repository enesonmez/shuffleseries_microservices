namespace ShuffleSeries.Catalog.Api.Contracts.Requests.Movies;

public sealed record UpdateMovieRequest(
    int TmdbId,
    string ImdbId,
    int ReleaseYear,
    int RuntimeMinutes,
    string? Director = null,
    double? ImdbRating = null,
    int? ImdbVotes = null,
    string? PosterUrl = null,
    string? BackdropUrl = null);
