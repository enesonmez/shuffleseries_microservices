using MediatR;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Application.Responses;

namespace ShuffleSeries.Catalog.Application.Features.Series.Queries.GetSeriesList;

internal sealed class GetSeriesListQueryHandler : 
    IRequestHandler<GetSeriesListQuery, PaginatedList<SeriesListItemResponse>>
{
    private readonly ISeriesRepository _seriesRepository;

    public GetSeriesListQueryHandler(ISeriesRepository seriesRepository)
    {
        _seriesRepository = seriesRepository;
    }

    public async Task<PaginatedList<SeriesListItemResponse>> Handle(GetSeriesListQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) =
            await _seriesRepository.GetPagedListAsync(request.Page, request.PageSize, cancellationToken);

        var mappedItems = items.Select(series => new SeriesListItemResponse(
            series.Id,
            series.Title,
            series.Description,
            series.IsIndependentEpisodes,
            series.CreatedAtUtc
        )).ToList();

        return new PaginatedList<SeriesListItemResponse>(mappedItems, totalCount, request.Page, request.PageSize);
    }
}