using MediatR;
using ShuffleSeries.Catalog.Application.Features.Movies.Responses;

namespace ShuffleSeries.Catalog.Application.Features.Movies.Queries.GetMovieById;

public sealed record GetMovieByIdQuery(Guid Id, string? Language = null) : IRequest<MovieDetailResponse>;
