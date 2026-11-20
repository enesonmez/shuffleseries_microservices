using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Domain.Exceptions;

public sealed class MovieNotFoundException : NotFoundException
{
    public MovieNotFoundException(Guid movieId)
        : base($"Movie with id '{movieId}' was not found.", "MOVIE_NOT_FOUND")
    {
    }
}
