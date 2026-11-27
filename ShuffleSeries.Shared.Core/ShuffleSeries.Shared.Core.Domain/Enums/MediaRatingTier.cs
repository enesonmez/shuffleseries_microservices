namespace ShuffleSeries.Shared.Core.Domain.Enums;

/// <summary>
/// Represents the 3-state user rating tier defined in PRD Section 5.4 and 6.2.
/// </summary>
public enum MediaRatingTier
{
    Disliked = -1,
    Liked = 1,
    DoubleThumbsUp = 2
}
