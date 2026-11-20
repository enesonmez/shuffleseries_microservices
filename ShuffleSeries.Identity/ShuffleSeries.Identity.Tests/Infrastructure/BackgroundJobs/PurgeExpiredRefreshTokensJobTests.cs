using Quartz;
using ShuffleSeries.Identity.Infrastructure.BackgroundJobs;

namespace ShuffleSeries.Identity.Tests.Infrastructure.BackgroundJobs;

public class PurgeExpiredRefreshTokensJobTests
{
    [Fact]
    public void PurgeExpiredRefreshTokensJob_ShouldHaveDisallowConcurrentExecution_AndImplementIJob()
    {
        // Assert
        typeof(IJob).IsAssignableFrom(typeof(PurgeExpiredRefreshTokensJob)).Should().BeTrue();
        Attribute.IsDefined(typeof(PurgeExpiredRefreshTokensJob), typeof(DisallowConcurrentExecutionAttribute)).Should().BeTrue();
        typeof(PurgeExpiredRefreshTokensJob).IsSealed.Should().BeTrue();
    }

    [Fact]
    public void RetentionDays_ShouldBe30Days() =>
        PurgeExpiredRefreshTokensJob.RetentionDays.Should().Be(30);
}
