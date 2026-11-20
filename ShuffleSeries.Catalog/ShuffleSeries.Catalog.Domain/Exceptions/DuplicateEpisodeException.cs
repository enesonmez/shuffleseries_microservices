using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Domain.Exceptions;

public sealed class DuplicateEpisodeException : ConflictException
{
    public DuplicateEpisodeException(int seasonNumber, int episodeNumber)
        : base($"Episode {episodeNumber} already exists in Season {seasonNumber}.", "DUPLICATE_EPISODE")
    {
    }
}
