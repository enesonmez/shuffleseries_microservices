using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Catalog.Domain.Entities;

public class MediaMood : BaseEntity
{
    public Guid MediaId { get; private set; }
    public Guid MoodId { get; private set; }

    public Mood? Mood { get; private set; }

    private MediaMood() : base(Guid.Empty) { }

    internal MediaMood(Guid mediaId, Guid moodId)
        : base(Guid.Empty)
    {
        if (moodId == Guid.Empty)
            throw new ArgumentException("MoodId cannot be empty.", nameof(moodId));

        MediaId = mediaId;
        MoodId = moodId;
    }
}
