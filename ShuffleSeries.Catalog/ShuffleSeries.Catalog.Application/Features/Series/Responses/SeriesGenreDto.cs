namespace ShuffleSeries.Catalog.Application.Features.Series.Responses;

public sealed record SeriesGenreDto(
    Guid GenreId,
    string Code,
    string Name);
