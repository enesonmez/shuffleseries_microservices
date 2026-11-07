using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Domain.Exceptions;

public sealed class GenreCodeAlreadyExistsException : ConflictException
{
    public GenreCodeAlreadyExistsException(string code)
        : base($"Genre with code '{code}' already exists.", "GENRE_CODE_ALREADY_EXISTS")
    {
    }
}
