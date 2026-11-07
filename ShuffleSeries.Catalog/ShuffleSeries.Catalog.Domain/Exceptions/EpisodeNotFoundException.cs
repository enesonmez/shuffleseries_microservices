using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Domain.Exceptions;

public sealed class EpisodeNotFoundException : NotFoundException
{
    public EpisodeNotFoundException(Guid episodeId)
        : base($"Episode with id '{episodeId}' was not found.", "EPISODE_NOT_FOUND")
    {
    }
}
