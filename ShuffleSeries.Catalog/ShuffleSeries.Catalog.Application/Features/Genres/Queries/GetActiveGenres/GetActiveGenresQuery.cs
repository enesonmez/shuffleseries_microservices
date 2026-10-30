using MediatR;
using ShuffleSeries.Catalog.Application.Features.Genres.Responses;

namespace ShuffleSeries.Catalog.Application.Features.Genres.Queries.GetActiveGenres;

public sealed record GetActiveGenresQuery(string? Language = null) : IRequest<IReadOnlyList<GenreResponse>>;
