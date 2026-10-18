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

    public async Task EnsureImdbIdIsUniqueAsync(string? imdbId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imdbId))
            return;

        var normalized = imdbId.Trim();
        var exists = await _seriesRepository.ExistsByImdbIdAsync(normalized, excludeId, cancellationToken);

        if (exists)
            throw new DuplicateImdbIdException(normalized);
    }
}
