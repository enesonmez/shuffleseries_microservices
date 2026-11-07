namespace ShuffleSeries.Catalog.Application.Features.Series.Queries.GetSeriesById;

public sealed record GetSeriesByIdQuery(Guid Id, string? Language = null);
