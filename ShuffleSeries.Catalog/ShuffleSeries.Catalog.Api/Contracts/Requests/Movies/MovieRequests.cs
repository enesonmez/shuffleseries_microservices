namespace ShuffleSeries.Catalog.Api.Contracts.Requests.Movies;

public sealed record CreateMovieRequest(
    int TmdbId,
    string ImdbId,
    int ReleaseYear,
    int RuntimeMinutes,
    string PosterUrl,
    string? BackdropUrl,
    string LanguageCode,
    string Title,
    string Overview,
    string? Tagline = null,
    string? Director = null,
    double? ImdbRating = null,
    int? ImdbVotes = null,
    IReadOnlyList<Guid>? PlatformIds = null,
    IReadOnlyList<Guid>? MoodIds = null,
    IReadOnlyList<Guid>? GenreIds = null);

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
