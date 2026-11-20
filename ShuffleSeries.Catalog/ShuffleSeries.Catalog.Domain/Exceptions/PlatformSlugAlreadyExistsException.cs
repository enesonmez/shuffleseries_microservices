using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Domain.Exceptions;

public sealed class PlatformSlugAlreadyExistsException : ConflictException
{
    public PlatformSlugAlreadyExistsException(string slug)
        : base($"Platform with slug '{slug}' already exists.", "PLATFORM_SLUG_ALREADY_EXISTS")
    {
    }
}
