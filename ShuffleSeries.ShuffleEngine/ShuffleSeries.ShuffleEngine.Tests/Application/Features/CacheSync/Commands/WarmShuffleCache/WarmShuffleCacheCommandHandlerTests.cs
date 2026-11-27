using Moq;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Application.Features.CacheSync.Commands.WarmShuffleCache;
using ShuffleSeries.ShuffleEngine.Application.Features.CacheSync.Responses;

namespace ShuffleSeries.ShuffleEngine.Tests.Application.Features.CacheSync.Commands.WarmShuffleCache;

public class WarmShuffleCacheCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldDelegateToWarmingService()
    {
        var warmingMock = new Mock<IShuffleCacheWarmingService>();
        var expected = new WarmShuffleCacheResponse(false, 10, 5, 20, 18, 42, DateTime.UtcNow);
        warmingMock
            .Setup(x => x.EnsureWarmedAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new WarmShuffleCacheCommandHandler(warmingMock.Object);
        var result = await handler.Handle(new WarmShuffleCacheCommand(ForceRebuild: true, "test"), CancellationToken.None);

        result.Should().Be(expected);
    }
}
