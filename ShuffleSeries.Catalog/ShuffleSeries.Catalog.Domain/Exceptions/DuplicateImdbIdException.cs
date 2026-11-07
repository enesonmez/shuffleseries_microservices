using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Domain.Exceptions;

public sealed class DuplicateImdbIdException : ConflictException
{
    public DuplicateImdbIdException(string imdbId)
        : base($"Media with IMDb ID '{imdbId}' already exists.", "DUPLICATE_IMDB_ID")
    {
    }
}
