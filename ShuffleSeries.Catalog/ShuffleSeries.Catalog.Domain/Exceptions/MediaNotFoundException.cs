using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Domain.Exceptions;

public sealed class MediaNotFoundException : NotFoundException
{
    public MediaNotFoundException(Guid mediaId)
        : base($"Media with id '{mediaId}' was not found.", "MEDIA_NOT_FOUND")
    {
    }
}
