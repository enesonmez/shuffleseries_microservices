using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Domain.Exceptions;

public sealed class GenreNotFoundException : NotFoundException
{
    public GenreNotFoundException(Guid genreId)
        : base($"Genre with id '{genreId}' was not found.", "GENRE_NOT_FOUND")
    {
    }
}
