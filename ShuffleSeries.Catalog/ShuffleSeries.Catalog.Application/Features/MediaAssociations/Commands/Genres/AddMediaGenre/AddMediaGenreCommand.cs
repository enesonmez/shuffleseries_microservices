using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.MediaAssociations.Commands.Genres.AddMediaGenre;

public sealed record AddMediaGenreCommand(Guid MediaId, Guid GenreId) : IRequest;
