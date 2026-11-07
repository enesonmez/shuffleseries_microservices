using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Domain.Exceptions;

public sealed class SeasonNotFoundException : NotFoundException
{
    public SeasonNotFoundException(Guid seasonId)
        : base($"Season with id '{seasonId}' was not found.", "SEASON_NOT_FOUND")
    {
    }
}
