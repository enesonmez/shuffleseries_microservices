namespace ShuffleSeries.ShuffleEngine.Domain.Constants;

public static class ShuffleDomainConstants
{
    public const double SafetyNetMinImdbRating = 7.5;
    public const int CooldownHours = 24;
    public const int AffinityRetentionDays = 30;

    public const double DoubleThumbsUpBoost = 0.30;
    public const double LikedBoost = 0.10;
    public const double OnboardingBoost = 0.15;
    public const double DislikedPenalty = -0.50;

    public const double MinFeatureWeight = -0.80;
    public const double MaxFeatureWeight = 1.50;

    public const double ImdbScoreWeight = 0.50;
    public const double PopularityScoreWeight = 0.30;
    public const double RecencyScoreWeight = 0.20;
}
