using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Domain.Exceptions;

public sealed class MoodCodeAlreadyExistsException : ConflictException
{
    public MoodCodeAlreadyExistsException(string code)
        : base($"Mood with code '{code}' already exists.", "MOOD_CODE_ALREADY_EXISTS")
    {
    }
}
