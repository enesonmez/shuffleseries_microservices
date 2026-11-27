using ShuffleSeries.Shared.Core.Domain.Enums;
using ShuffleSeries.ShuffleEngine.Domain.Constants;
using ShuffleSeries.ShuffleEngine.Domain.Models;

namespace ShuffleSeries.ShuffleEngine.Tests.Domain.Models;

public class UserFeatureVectorTests
{
    [Fact]
    public void GetRatingDelta_ShouldMatchPrdFeedbackWeights()
    {
        UserFeatureVector.GetRatingDelta(MediaRatingTier.DoubleThumbsUp).Should().Be(0.30);
        UserFeatureVector.GetRatingDelta(MediaRatingTier.Liked).Should().Be(0.10);
        UserFeatureVector.GetRatingDelta(MediaRatingTier.Disliked).Should().Be(-0.50);
    }

    [Fact]
    public void ClampWeight_ShouldEnforceBounds()
    {
        UserFeatureVector.ClampWeight(9.9).Should().Be(ShuffleDomainConstants.MaxFeatureWeight);
        UserFeatureVector.ClampWeight(-9.9).Should().Be(ShuffleDomainConstants.MinFeatureWeight);
    }

    [Fact]
    public void CalculateSuitabilityMultiplier_ShouldApplyBoostsAndDislikePenalties()
    {
        var userId = Guid.NewGuid();
        var favMoodId = Guid.NewGuid();
        var dislikedGenreId = Guid.NewGuid();
        var onboardingMoodId = Guid.NewGuid();

        var vector = new UserFeatureVector(
            userId,
            new Dictionary<Guid, double> { [favMoodId] = 0.30 },
            new Dictionary<Guid, double> { [dislikedGenreId] = -0.50 },
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["christopher nolan"] = 0.30 },
            new HashSet<Guid> { onboardingMoodId },
            DateTime.UtcNow);

        // Positive boost only (+30% -> 1.30x)
        vector.CalculateSuitabilityMultiplier([favMoodId], [], "Christopher Nolan")
            .Should().Be(1.30);

        // Onboarding boost only (+15% -> 1.15x)
        vector.CalculateSuitabilityMultiplier([onboardingMoodId], [], null)
            .Should().Be(1.15);

        // Penalty only (-50% -> 0.50x)
        vector.CalculateSuitabilityMultiplier([], [dislikedGenreId], null)
            .Should().Be(0.50);

        // Boost (+30%) combined with Penalty (-50%) -> 1.30 * 0.50 = 0.65
        vector.CalculateSuitabilityMultiplier([favMoodId], [dislikedGenreId], null)
            .Should().Be(0.65);
    }
}
