using MediatR;
using ShuffleSeries.Shared.Core.Application.Responses;

namespace ShuffleSeries.Catalog.Application.Features.Series.Queries.GetSeriesByList;

public record GetSeriesListQuery(int Page = 1, int PageSize = 10) : IRequest<PaginatedList<SeriesListItemResponse>>;

public record SeriesListItemResponse(
    Guid Id, 
    string Title, 
    string Description, 
    bool IsIndependentEpisodes,
    DateTime CreatedAtUtc
);