using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Domain.Exceptions;

public sealed class DuplicateMediaPlatformException : ConflictException
{
    public DuplicateMediaPlatformException(Guid mediaId, Guid platformId)
        : base($"Media '{mediaId}' is already associated with platform '{platformId}'.", "DUPLICATE_MEDIA_PLATFORM")
    {
    }
}
