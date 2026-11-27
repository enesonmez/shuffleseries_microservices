using Moq;
using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Application.Consumers;

namespace ShuffleSeries.ShuffleEngine.Tests.Application.Consumers;

public class EpisodeDeletedEventHandlerTests
{
    [Fact]
    public async Task Handle_ShouldRemoveEpisodeFromPool()
    {
        var epId = Guid.NewGuid();
        var evt = new EpisodeDeletedEvent(Guid.NewGuid(), epId, DateTime.UtcNow);

        var inboxMock = new Mock<IInboxIdempotencyService>();
        inboxMock
            .Setup(x => x.ExecuteIdempotentAsync(evt, It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<EpisodeDeletedEvent, Func<CancellationToken, Task>, CancellationToken>(async (_, action, ct) =>
            {
                await action(ct);
                return true;
            });

        var poolMock = new Mock<IShufflePoolStore>();
        var handler = new EpisodeDeletedEventHandler(inboxMock.Object, poolMock.Object);

        await handler.Handle(evt, CancellationToken.None);

        poolMock.Verify(x => x.RemoveEpisodeAsync(epId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
