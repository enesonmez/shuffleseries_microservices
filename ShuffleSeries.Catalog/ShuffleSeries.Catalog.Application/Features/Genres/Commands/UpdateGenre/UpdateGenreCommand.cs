using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Genres.Commands.UpdateGenre;

public sealed record UpdateGenreCommand(
    Guid Id,
    string Code,
    int DisplayOrder = 0) : IRequest;
