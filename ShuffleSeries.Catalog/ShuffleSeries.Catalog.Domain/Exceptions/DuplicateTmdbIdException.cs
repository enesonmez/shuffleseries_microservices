using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Domain.Exceptions;

public sealed class DuplicateTmdbIdException : ConflictException
{
    public DuplicateTmdbIdException(int tmdbId)
        : base($"Media with TMDb ID '{tmdbId}' already exists.", "DUPLICATE_TMDB_ID")
    {
    }
}
