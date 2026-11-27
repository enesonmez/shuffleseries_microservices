using ShuffleSeries.ShuffleEngine.Domain.Services;

namespace ShuffleSeries.ShuffleEngine.Tests.Domain.Services;

public class BaseQualityScoreCalculatorTests
{
    [Fact]
    public void CalculateMediaScore_ForModernMasterpiece_ShouldReturnHighBoundedScore()
    {
        var score = BaseQualityScoreCalculator.CalculateMediaScore(
            imdbRating: 8.8,
            imdbVotes: 2_500_000,
            releaseYear: 2024,
            currentYear: 2026);

        score.Should().BeGreaterThan(8.5).And.BeLessThanOrEqualTo(10.0);
    }

    [Fact]
    public void CalculateMediaScore_WhenUnratedAndZeroVotes_ShouldFallbackDeterministically()
    {
        var score = BaseQualityScoreCalculator.CalculateMediaScore(
            imdbRating: null,
            imdbVotes: 0,
            releaseYear: 1995,
            currentYear: 2026);

        score.Should().BeGreaterThan(0.1).And.BeLessThan(10.0);
    }

    [Fact]
    public void CalculateEpisodeScore_ShouldProduceValidPositiveScore()
    {
        var score = BaseQualityScoreCalculator.CalculateEpisodeScore(8.6, 2016, 2026);
        score.Should().BeGreaterThan(7.0).And.BeLessThanOrEqualTo(10.0);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(7.49, false)]
    [InlineData(7.50, true)]
    [InlineData(8.80, true)]
    public void IsSafetyNetEligible_ShouldEnforceSevenPointFiveThreshold(double? rating, bool expected) =>
        BaseQualityScoreCalculator.IsSafetyNetEligible(rating).Should().Be(expected);
}
