using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Domain.Exceptions;

public sealed class SeriesNotFoundException : NotFoundException
{
    public SeriesNotFoundException(Guid seriesId)
        : base($"Series with id '{seriesId}' was not found.", "SERIES_NOT_FOUND")
    {
    }
}
