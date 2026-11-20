using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Domain.Exceptions;

public sealed class DuplicateMediaGenreException : ConflictException
{
    public DuplicateMediaGenreException(Guid mediaId, Guid genreId)
        : base($"Media '{mediaId}' is already associated with genre '{genreId}'.", "DUPLICATE_MEDIA_GENRE")
    {
    }
}
