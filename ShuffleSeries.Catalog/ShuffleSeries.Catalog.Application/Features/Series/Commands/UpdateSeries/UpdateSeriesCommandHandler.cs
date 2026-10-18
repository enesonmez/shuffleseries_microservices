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

        if (!string.IsNullOrWhiteSpace(request.ImdbId))
        {
            var normalizedImdbId = request.ImdbId.Trim();
            if (await _seriesRepository.ExistsByImdbIdAsync(normalizedImdbId, series.Id, cancellationToken))
            {
                throw new DuplicateImdbIdException(normalizedImdbId);
            }
        }

        series.UpdateDetails(
            request.ReleaseYear,
            request.SeriesType,
            request.TotalSeasons,
            request.ImdbId,
            request.ImdbRating,
            request.ImdbVotes);

        _seriesRepository.Update(series);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
