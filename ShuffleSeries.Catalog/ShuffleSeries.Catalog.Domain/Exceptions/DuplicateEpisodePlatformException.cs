using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Domain.Exceptions;

public sealed class DuplicateEpisodePlatformException : ConflictException
{
    public DuplicateEpisodePlatformException(Guid episodeId, Guid platformId)
        : base($"Episode '{episodeId}' is already associated with platform '{platformId}'.", "DUPLICATE_EPISODE_PLATFORM")
    {
    }
}
