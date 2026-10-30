using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Movies.Commands.UpdateMovie;

public sealed record UpdateMovieCommand(
    Guid Id,
    int TmdbId,
    string ImdbId,
    int ReleaseYear,
    int RuntimeMinutes,
    string? Director = null,
    double? ImdbRating = null,
    int? ImdbVotes = null,
    string? PosterUrl = null,
    string? BackdropUrl = null) : IRequest;
