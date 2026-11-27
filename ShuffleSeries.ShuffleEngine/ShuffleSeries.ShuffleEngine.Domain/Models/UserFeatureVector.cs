using ShuffleSeries.Shared.Core.Domain.Enums;
using ShuffleSeries.ShuffleEngine.Domain.Constants;

namespace ShuffleSeries.ShuffleEngine.Domain.Models;

/// <summary>
/// Represents a user's behavioral affinity feature vector stored in Redis (`shuffle:user:{userId}:affinity`).
/// Encapsulates mood, genre, director, and onboarding weights per PRD Section 6.2 &amp; 6.4.
/// </summary>
public sealed record UserFeatureVector(
    Guid UserId,
    IReadOnlyDictionary<Guid, double> MoodWeights,
    IReadOnlyDictionary<Guid, double> GenreWeights,
    IReadOnlyDictionary<string, double> DirectorWeights,
    IReadOnlySet<Guid> OnboardingMoodIds,
    DateTime UpdatedAtUtc)
{
    public static UserFeatureVector Empty(Guid userId, DateTime utcNow) =>
        new(
            userId,
            new Dictionary<Guid, double>(),
            new Dictionary<Guid, double>(),
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase),
            new HashSet<Guid>(),
            utcNow);

    public static double GetRatingDelta(MediaRatingTier tier) => tier switch
    {
        MediaRatingTier.DoubleThumbsUp => ShuffleDomainConstants.DoubleThumbsUpBoost,
        MediaRatingTier.Liked => ShuffleDomainConstants.LikedBoost,
        MediaRatingTier.Disliked => ShuffleDomainConstants.DislikedPenalty,
        _ => 0.0
    };

    public static string NormalizeDirectorKey(string director) =>
        director.Trim().ToLowerInvariant();

    public static double ClampWeight(double value) =>
        Math.Round(Math.Clamp(value, ShuffleDomainConstants.MinFeatureWeight, ShuffleDomainConstants.MaxFeatureWeight), 4);

    /// <summary>
    /// Computes the composite `(1 + AffinityBoost) * PenaltyFactor` multiplier per PRD Section 6.2.
    /// </summary>
    public double CalculateSuitabilityMultiplier(
        IReadOnlyCollection<Guid> moodIds,
        IReadOnlyCollection<Guid> genreIds,
        string? director)
    {
        var maxPositiveBoost = 0.0;
        var hasPenalty = false;

        EvaluateMoodSignals(moodIds, ref maxPositiveBoost, ref hasPenalty);
        EvaluateDictionarySignals(genreIds, GenreWeights, ref maxPositiveBoost, ref hasPenalty);
        EvaluateDirectorSignal(director, ref maxPositiveBoost, ref hasPenalty);

        var boostFactor = 1.0 + Math.Min(maxPositiveBoost, ShuffleDomainConstants.DoubleThumbsUpBoost + ShuffleDomainConstants.OnboardingBoost);
        var penaltyFactor = hasPenalty ? 1.0 + ShuffleDomainConstants.DislikedPenalty : 1.0;

        return Math.Round(boostFactor * penaltyFactor, 4);
    }

    private void EvaluateMoodSignals(IReadOnlyCollection<Guid> moodIds, ref double maxPositiveBoost, ref bool hasPenalty)
    {
        foreach (var moodId in moodIds)
        {
            if (OnboardingMoodIds.Contains(moodId))
            {
                maxPositiveBoost = Math.Max(maxPositiveBoost, ShuffleDomainConstants.OnboardingBoost);
            }

            if (MoodWeights.TryGetValue(moodId, out var weight))
            {
                MergeSignalWeight(weight, ref maxPositiveBoost, ref hasPenalty);
            }
        }
    }

    private static void EvaluateDictionarySignals<TKey>(
        IReadOnlyCollection<TKey> keys,
        IReadOnlyDictionary<TKey, double> weights,
        ref double maxPositiveBoost,
        ref bool hasPenalty)
        where TKey : notnull
    {
        foreach (var key in keys)
        {
            if (weights.TryGetValue(key, out var weight))
            {
                MergeSignalWeight(weight, ref maxPositiveBoost, ref hasPenalty);
            }
        }
    }

    private void EvaluateDirectorSignal(string? director, ref double maxPositiveBoost, ref bool hasPenalty)
    {
        if (string.IsNullOrWhiteSpace(director))
        {
            return;
        }

        if (DirectorWeights.TryGetValue(NormalizeDirectorKey(director), out var dirWeight))
        {
            MergeSignalWeight(dirWeight, ref maxPositiveBoost, ref hasPenalty);
        }
    }

    private static void MergeSignalWeight(double weight, ref double maxPositiveBoost, ref bool hasPenalty)
    {
        if (weight > 0)
        {
            maxPositiveBoost = Math.Max(maxPositiveBoost, weight);
        }
        else if (weight < 0)
        {
            hasPenalty = true;
        }
    }
}
