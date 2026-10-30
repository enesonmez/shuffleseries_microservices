using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Genres.Commands.CreateGenre;

public sealed record CreateGenreCommand(
    string Code,
    string Name,
    string LanguageCode,
    string? Description = null,
    int DisplayOrder = 0,
    bool IsActive = true) : IRequest<Guid>;
