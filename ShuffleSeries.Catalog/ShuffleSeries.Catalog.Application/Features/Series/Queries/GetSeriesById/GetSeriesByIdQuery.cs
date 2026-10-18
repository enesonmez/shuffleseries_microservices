using MediatR;
using ShuffleSeries.Catalog.Application.Features.Series.Responses;

namespace ShuffleSeries.Catalog.Application.Features.Series.Queries.GetSeriesById;

public sealed record GetSeriesByIdQuery(Guid Id) : IRequest<SeriesResponse>;
