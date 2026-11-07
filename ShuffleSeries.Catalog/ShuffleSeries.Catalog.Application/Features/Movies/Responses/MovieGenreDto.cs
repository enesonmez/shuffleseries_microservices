namespace ShuffleSeries.Catalog.Application.Features.Movies.Responses;

public sealed record MovieGenreDto(
    Guid GenreId,
    string Code,
    string Name);
