using Quartz;
using ShuffleSeries.Identity.Infrastructure.BackgroundJobs;

namespace ShuffleSeries.Identity.Tests.Infrastructure.BackgroundJobs;

public class PurgeAbandonedGuestAccountsJobTests
{
    [Fact]
    public void PurgeAbandonedGuestAccountsJob_ShouldHaveDisallowConcurrentExecution_AndImplementIJob()
    {
        // Assert
        typeof(IJob).IsAssignableFrom(typeof(PurgeAbandonedGuestAccountsJob)).Should().BeTrue();
        Attribute.IsDefined(typeof(PurgeAbandonedGuestAccountsJob), typeof(DisallowConcurrentExecutionAttribute)).Should().BeTrue();
        typeof(PurgeAbandonedGuestAccountsJob).IsSealed.Should().BeTrue();
    }

    [Fact]
    public void InactiveDaysThreshold_ShouldBe60Days() =>
        PurgeAbandonedGuestAccountsJob.InactiveDaysThreshold.Should().Be(60);
}
