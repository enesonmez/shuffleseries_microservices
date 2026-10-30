using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Genres.Commands.DeleteGenre;

public sealed record DeleteGenreCommand(Guid Id) : IRequest;
