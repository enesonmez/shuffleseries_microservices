using MediatR;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Series.Commands.DeleteSeries;

internal sealed class DeleteSeriesCommandHandler : IRequestHandler<DeleteSeriesCommand>
{
    private readonly ISeriesRepository _seriesRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSeriesCommandHandler(ISeriesRepository seriesRepository, IUnitOfWork unitOfWork)
    {
        _seriesRepository = seriesRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteSeriesCommand request, CancellationToken cancellationToken)
    {
        var series = await _seriesRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new SeriesNotFoundException(request.Id);

        series.Delete();
        _seriesRepository.Delete(series);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
