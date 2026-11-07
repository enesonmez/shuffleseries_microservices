using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Domain.Exceptions;

public sealed class DuplicateMediaMoodException : ConflictException
{
    public DuplicateMediaMoodException(Guid mediaId, Guid moodId)
        : base($"Media '{mediaId}' is already associated with mood '{moodId}'.", "DUPLICATE_MEDIA_MOOD")
    {
    }
}
