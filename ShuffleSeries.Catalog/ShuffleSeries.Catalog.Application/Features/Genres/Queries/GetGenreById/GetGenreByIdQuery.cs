namespace ShuffleSeries.Catalog.Application.Features.Genres.Queries.GetGenreById;

public sealed record GetGenreByIdQuery(Guid Id, string? Language = null);
