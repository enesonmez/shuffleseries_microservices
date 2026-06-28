using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Domain.Services;

public sealed class SeriesDomainService
{
    private readonly ISeriesRepository _seriesRepository;
    
    public SeriesDomainService(ISeriesRepository seriesRepository)
    {
        _seriesRepository = seriesRepository;
    }

    public async Task EnsureTitleIsUniqueAsync(string title, CancellationToken cancellationToken = default)
    {
        var exists = await _seriesRepository.ExistsByTitleAsync(title, cancellationToken);
        
        if (exists)
            throw new SeriesTitleAlreadyExistsException(title);
    }
}