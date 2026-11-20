using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Domain.Exceptions;

public sealed class MoodNotFoundException : NotFoundException
{
    public MoodNotFoundException(Guid moodId)
        : base($"Mood with id '{moodId}' was not found.", "MOOD_NOT_FOUND")
    {
    }
}
