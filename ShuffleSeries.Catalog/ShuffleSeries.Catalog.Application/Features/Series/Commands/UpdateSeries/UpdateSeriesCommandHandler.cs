using MediatR;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Series.Commands.UpdateSeries;

public sealed class UpdateSeriesCommandHandler : IRequestHandler<UpdateSeriesCommand>
{
    private readonly ISeriesRepository _seriesRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSeriesCommandHandler(
        ISeriesRepository seriesRepository,
        IUnitOfWork unitOfWork)
    {
        _seriesRepository = seriesRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateSeriesCommand request, CancellationToken cancellationToken)
    {
        var series = await _seriesRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new SeriesNotFoundException(request.Id);

        if (await _seriesRepository.ExistsByTmdbIdAsync(request.TmdbId, series.Id, cancellationToken))
        {
            throw new DuplicateTmdbIdException(request.TmdbId);
        }

        if (await _seriesRepository.ExistsByImdbIdAsync(request.ImdbId, series.Id, cancellationToken))
        {
            throw new DuplicateImdbIdException(request.ImdbId);
        }

        series.UpdateDetails(
            request.TmdbId,
            request.ImdbId,
            request.ReleaseYear,
            request.SeriesType,
            request.TotalSeasons,
            request.ImdbRating,
            request.ImdbVotes,
            request.PosterUrl,
            request.BackdropUrl);

        _seriesRepository.Update(series);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
