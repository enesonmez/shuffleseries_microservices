using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Application.Features.CacheSync.Responses;
using ShuffleSeries.ShuffleEngine.Domain.Exceptions;
using ShuffleSeries.ShuffleEngine.Infrastructure.BackgroundJobs;

namespace ShuffleSeries.ShuffleEngine.Tests.Infrastructure.BackgroundJobs;

public class EnsureShuffleCacheWarmJobTests
{
    [Fact]
    public async Task Execute_ShouldCallEnsureWarmedAsync()
    {
        var warmMock = new Mock<IShuffleCacheWarmingService>();
        warmMock.Setup(x => x.EnsureWarmedAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WarmShuffleCacheResponse(true, 1, 1, 1, 3, 1, DateTime.UtcNow));

        var job = new EnsureShuffleCacheWarmJob(warmMock.Object, NullLogger<EnsureShuffleCacheWarmJob>.Instance);
        await job.Execute(Mock.Of<IJobExecutionContext>());

        warmMock.Verify(x => x.EnsureWarmedAsync(false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_WhenCatalogSyncFailsTransiently_ShouldSwallowWithoutCrashingScheduler()
    {
        var warmMock = new Mock<IShuffleCacheWarmingService>();
        warmMock.Setup(x => x.EnsureWarmedAsync(false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CatalogSyncFailedException("Catalog booting"));

        var job = new EnsureShuffleCacheWarmJob(warmMock.Object, NullLogger<EnsureShuffleCacheWarmJob>.Instance);

        var act = async () => await job.Execute(Mock.Of<IJobExecutionContext>());
        await act.Should().NotThrowAsync();
    }
}
