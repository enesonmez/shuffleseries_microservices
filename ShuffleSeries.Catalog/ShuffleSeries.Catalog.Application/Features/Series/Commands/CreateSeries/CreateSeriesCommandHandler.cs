using MediatR;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Catalog.Domain.Services;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Series.Commands.CreateSeries;

public sealed class CreateSeriesCommandHandler : IRequestHandler<CreateSeriesCommand, Guid>
{
    private readonly ISeriesRepository _seriesRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SeriesDomainService _seriesDomainService;

    public CreateSeriesCommandHandler(
        ISeriesRepository seriesRepository,
        IUnitOfWork unitOfWork,
        SeriesDomainService seriesDomainService)
    {
        _seriesRepository = seriesRepository;
        _unitOfWork = unitOfWork;
        _seriesDomainService = seriesDomainService;
    }

    public async Task<Guid> Handle(CreateSeriesCommand request, CancellationToken cancellationToken)
    {
        await _seriesDomainService.EnsureTitleIsUniqueAsync(request.Title, cancellationToken);
        await _seriesDomainService.EnsureImdbIdIsUniqueAsync(request.ImdbId, cancellationToken: cancellationToken);

        var series = Domain.Entities.Series.Create(
            request.ReleaseYear,
            request.SeriesType,
            request.PosterUrl,
            request.BackdropUrl,
            request.ImdbId,
            request.ImdbRating,
            request.ImdbVotes,
            request.TotalSeasons);

        series.AddOrUpdateTranslation(
            request.LanguageCode,
            request.Title,
            request.Overview,
            request.Tagline);

        _seriesRepository.Add(series);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return series.Id;
    }
}
