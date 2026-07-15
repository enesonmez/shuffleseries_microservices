using MediatR;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Catalog.Domain.Services;
using ShuffleSeries.Shared.Core.Domain.Repositories;
using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Application.Features.Series.Commands.UpdateSeries;

internal sealed class UpdateSeriesCommandHandler : IRequestHandler<UpdateSeriesCommand>
{
    private readonly ISeriesRepository _seriesRepository;
    private readonly SeriesDomainService _seriesDomainService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSeriesCommandHandler(
        ISeriesRepository seriesRepository, 
        SeriesDomainService seriesDomainService,
        IUnitOfWork unitOfWork)
    {
        _seriesRepository = seriesRepository;
        _seriesDomainService = seriesDomainService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateSeriesCommand request, CancellationToken cancellationToken)
    {
        var series = await _seriesRepository.GetByIdAsync(request.Id, cancellationToken);

        if (series is null)
        {
            throw new NotFoundException($"Series with ID {request.Id} was not found.");
        }
        
        if (!series.Title.Equals(request.Title, StringComparison.OrdinalIgnoreCase))
        {
            await _seriesDomainService.EnsureTitleIsUniqueAsync(request.Title, cancellationToken);
        }

        series.Update(
            request.Title,
            request.Description,
            request.IsIndependentEpisodes
        );
        
        _seriesRepository.Update(series);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}