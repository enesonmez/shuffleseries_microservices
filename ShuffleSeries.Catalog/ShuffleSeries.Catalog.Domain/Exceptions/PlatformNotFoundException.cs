using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Domain.Exceptions;

public sealed class PlatformNotFoundException : NotFoundException
{
    public PlatformNotFoundException(Guid platformId)
        : base($"Platform with id '{platformId}' was not found.", "PLATFORM_NOT_FOUND")
    {
    }
}
