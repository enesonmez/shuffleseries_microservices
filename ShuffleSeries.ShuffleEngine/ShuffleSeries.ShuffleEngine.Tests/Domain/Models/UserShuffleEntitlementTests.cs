using ShuffleSeries.ShuffleEngine.Domain.Models;

namespace ShuffleSeries.ShuffleEngine.Tests.Domain.Models;

public class UserShuffleEntitlementTests
{
    [Fact]
    public void IsCurrentlyPremium_ShouldRespectFlagAndExpiration()
    {
        var now = new DateTime(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();

        var active = new UserShuffleEntitlement(userId, true, "VIP_ANNUAL", now.AddDays(30), now.Ticks, now);
        active.IsCurrentlyPremium(now).Should().BeTrue();

        var expired = new UserShuffleEntitlement(userId, true, "VIP_MONTHLY", now.AddMinutes(-1), now.Ticks, now);
        expired.IsCurrentlyPremium(now).Should().BeFalse();

        var cancelled = new UserShuffleEntitlement(userId, false, null, now.AddDays(10), now.Ticks, now);
        cancelled.IsCurrentlyPremium(now).Should().BeFalse();
    }
}
