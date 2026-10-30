using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Movies.Commands.DeleteMovie;

public sealed record DeleteMovieCommand(Guid Id) : IRequest;
