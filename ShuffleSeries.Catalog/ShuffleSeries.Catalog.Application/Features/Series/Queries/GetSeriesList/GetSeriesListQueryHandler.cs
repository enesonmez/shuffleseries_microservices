using MediatR;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Application.Responses;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

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

        var mappedItems = items.Select(series =>
        {
            var title = series.Translations.FirstOrDefault(t => t.LanguageCode == LanguageCode.Turkish)?.Title
                ?? series.Translations.FirstOrDefault()?.Title
                ?? "Untitled";
            return new SeriesListItemResponse(
                series.Id,
                title,
                series.ReleaseYear,
                series.SeriesType,
                series.TotalSeasons,
                series.PosterUrl,
                series.CreatedAtUtc
            );
        }).ToList();

        return new PaginatedList<SeriesListItemResponse>(mappedItems, totalCount, request.Page, request.PageSize);
    }
}
