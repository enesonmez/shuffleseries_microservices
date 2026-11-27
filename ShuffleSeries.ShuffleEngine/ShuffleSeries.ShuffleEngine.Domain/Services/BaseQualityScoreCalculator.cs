using ShuffleSeries.ShuffleEngine.Domain.Constants;

namespace ShuffleSeries.ShuffleEngine.Domain.Services;

/// <summary>
/// Computes the deterministic PRD Section 6.2 Base Quality Score on a [0..10] scale:
/// (IMDb * 0.5) + (Popularity * 0.3) + (Recency * 0.2).
/// </summary>
public static class BaseQualityScoreCalculator
{
    private const double DefaultUnratedImdb = 6.5;
    private const double MaxLogVotes = 6.5; // log10(3,162,277) ~= 6.5 -> 10.0 popularity ceiling
    private const int ClassicFloorYear = 1970;

    public static double CalculateMediaScore(
        double? imdbRating,
        int? imdbVotes,
        int releaseYear,
        int currentYear)
    {
        var normalizedImdb = Math.Clamp(imdbRating ?? DefaultUnratedImdb, 0.0, 10.0);

        var safeVotes = Math.Max(0, imdbVotes ?? 0);
        var normalizedPopularity = safeVotes == 0
            ? 5.0
            : Math.Clamp((Math.Log10(safeVotes + 1) / MaxLogVotes) * 10.0, 0.0, 10.0);

        var normalizedRecency = CalculateRecencyComponent(releaseYear, currentYear);

        var rawScore = (normalizedImdb * ShuffleDomainConstants.ImdbScoreWeight)
                       + (normalizedPopularity * ShuffleDomainConstants.PopularityScoreWeight)
                       + (normalizedRecency * ShuffleDomainConstants.RecencyScoreWeight);

        return Math.Round(Math.Clamp(rawScore, 0.1, 10.0), 4);
    }

    public static double CalculateEpisodeScore(
        double? imdbRating,
        int releaseYear,
        int currentYear) =>
        CalculateMediaScore(imdbRating, imdbVotes: 25_000, releaseYear, currentYear);

    public static bool IsSafetyNetEligible(double? imdbRating) =>
        imdbRating.HasValue && imdbRating.Value >= ShuffleDomainConstants.SafetyNetMinImdbRating;

    private static double CalculateRecencyComponent(int releaseYear, int currentYear)
    {
        var effectiveCurrentYear = Math.Max(currentYear, ClassicFloorYear + 1);
        var clampedYear = Math.Clamp(releaseYear, ClassicFloorYear, effectiveCurrentYear);
        var span = (double)(effectiveCurrentYear - ClassicFloorYear);
        return Math.Clamp(((clampedYear - ClassicFloorYear) / span) * 10.0, 0.0, 10.0);
    }
}
