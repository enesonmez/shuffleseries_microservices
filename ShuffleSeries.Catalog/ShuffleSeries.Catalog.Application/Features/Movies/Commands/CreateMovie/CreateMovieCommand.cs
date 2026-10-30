using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Movies.Commands.CreateMovie;

public sealed record CreateMovieCommand(
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
    IReadOnlyList<Guid>? GenreIds = null) : IRequest<Guid>;
