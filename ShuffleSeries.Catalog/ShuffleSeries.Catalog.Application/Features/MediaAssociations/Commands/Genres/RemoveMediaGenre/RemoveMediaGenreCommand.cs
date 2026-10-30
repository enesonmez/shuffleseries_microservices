using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Genres.RemoveMediaGenre;

public sealed record RemoveMediaGenreCommand(Guid MediaId, Guid GenreId) : IRequest;
