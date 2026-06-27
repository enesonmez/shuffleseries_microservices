using MediatR;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Repositories;

namespace ShuffleSeries.Catalog.Application.Features.Series.Commands.CreateSeries;

internal sealed class CreateSeriesCommandHandler : IRequestHandler<CreateSeriesCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISeriesRepository _seriesRepository;
    
    public CreateSeriesCommandHandler(IUnitOfWork unitOfWork, ISeriesRepository seriesRepository)
    {
        _unitOfWork = unitOfWork;
        _seriesRepository = seriesRepository;
    }
    
    public async Task<Guid> Handle(CreateSeriesCommand request, CancellationToken cancellationToken)
    {
        var series = Domain.Entities.Series.Create(request.Title, request.Description, request.IsIndependentEpisodes);
        
        _seriesRepository.Add(series);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return await Task.FromResult(series.Id);
    }
}