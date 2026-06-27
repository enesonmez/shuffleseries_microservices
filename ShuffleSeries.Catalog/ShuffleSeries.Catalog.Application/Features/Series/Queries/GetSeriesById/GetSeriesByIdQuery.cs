using MediatR;

namespace ShuffleSeries.Catalog.Application.Features.Series.Queries.GetSeriesById;

public record GetSeriesByIdQuery(Guid Id) : IRequest<SeriesResponse?>;

public record SeriesResponse(
    Guid Id, 
    string Title, 
    string Description, 
    bool IsIndependentEpisodes,
    DateTime CreatedAtUtc
);