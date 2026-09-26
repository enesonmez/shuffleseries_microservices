using MediatR;
using ShuffleSeries.Shared.Core.Application.Requests;
using ShuffleSeries.Shared.Core.Application.Responses;

namespace ShuffleSeries.Catalog.Application.Features.Series.Queries.GetSeriesList;

public record GetSeriesListQuery : PaginationRequest, IRequest<PaginatedList<SeriesListItemResponse>>
{
    public GetSeriesListQuery(int? page = 1, int? pageSize = 10)
        : base(page, pageSize)
    {
    }
}

public record SeriesListItemResponse(
    Guid Id,
    string Title,
    string Description,
    bool IsIndependentEpisodes,
    DateTime CreatedAtUtc
);