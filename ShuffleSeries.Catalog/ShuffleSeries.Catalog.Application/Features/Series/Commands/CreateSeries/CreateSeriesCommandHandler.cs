using MediatR;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Catalog.Domain.Services;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Series.Commands.CreateSeries;

internal sealed class CreateSeriesCommandHandler : IRequestHandler<CreateSeriesCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISeriesRepository _seriesRepository;
    private readonly SeriesDomainService _seriesDomainService;

    public CreateSeriesCommandHandler(IUnitOfWork unitOfWork, ISeriesRepository seriesRepository,
        SeriesDomainService seriesDomainService)
    {
        _unitOfWork = unitOfWork;
        _seriesRepository = seriesRepository;
        _seriesDomainService = seriesDomainService;
    }

    public async Task<Guid> Handle(CreateSeriesCommand request, CancellationToken cancellationToken)
    {
        await _seriesDomainService.EnsureTitleIsUniqueAsync(request.Title, cancellationToken);
        
        var series = Domain.Entities.Series.Create(
            request.Title, 
            request.Description, 
            request.IsIndependentEpisodes
        );

        _seriesRepository.Add(series);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return series.Id;
    }
}