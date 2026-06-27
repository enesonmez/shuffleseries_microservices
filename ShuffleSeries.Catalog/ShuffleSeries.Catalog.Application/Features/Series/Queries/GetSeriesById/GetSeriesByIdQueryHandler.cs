using MediatR;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Series.Queries.GetSeriesById;

internal sealed class GetSeriesByIdQueryHandler : IRequestHandler<GetSeriesByIdQuery, SeriesResponse?>
{
    private readonly ISeriesRepository _seriesRepository;
    
    public GetSeriesByIdQueryHandler(ISeriesRepository seriesRepository)
    {
        _seriesRepository = seriesRepository;
    }
    
    public async Task<SeriesResponse?> Handle(GetSeriesByIdQuery request, CancellationToken cancellationToken)
    {
        var series = await _seriesRepository.GetByIdAsync(request.Id, cancellationToken);

        if (series is null)
        {
            return null;
        }

        return new SeriesResponse(
            series.Id,
            series.Title,
            series.Description,
            series.IsIndependentEpisodes,
            series.CreatedAtUtc
        );
    }
}