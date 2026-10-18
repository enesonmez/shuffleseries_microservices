using MediatR;
using ShuffleSeries.Catalog.Application.Features.Series.Responses;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Series.Queries.GetSeriesById;

public sealed class GetSeriesByIdQueryHandler : IRequestHandler<GetSeriesByIdQuery, SeriesResponse>
{
    private readonly ISeriesRepository _seriesRepository;

    public GetSeriesByIdQueryHandler(ISeriesRepository seriesRepository)
    {
        _seriesRepository = seriesRepository;
    }

    public async Task<SeriesResponse> Handle(GetSeriesByIdQuery request, CancellationToken cancellationToken)
    {
        var series = await _seriesRepository.GetByIdWithTranslationsAsync(request.Id, cancellationToken)
            ?? throw new SeriesNotFoundException(request.Id);

        var translations = series.Translations
            .Select(t => new SeriesTranslationResponse(t.LanguageCode, t.Title, t.Overview, t.Tagline))
            .ToList();

        return new SeriesResponse(
            series.Id,
            series.ReleaseYear,
            series.SeriesType,
            series.TotalSeasons,
            series.PosterUrl,
            series.BackdropUrl,
            series.ImdbId,
            series.ImdbRating,
            series.ImdbVotes,
            translations);
    }
}
