using Moq;
using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Application.Consumers;

namespace ShuffleSeries.ShuffleEngine.Tests.Application.Consumers;

public class MediaDeletedEventHandlerTests
{
    [Fact]
    public async Task Handle_ShouldRemoveMediaFromPoolIdempotently()
    {
        var mediaId = Guid.NewGuid();
        var evt = new MediaDeletedEvent(Guid.NewGuid(), mediaId, "Series", DateTime.UtcNow);

        var inboxMock = new Mock<IInboxIdempotencyService>();
        inboxMock
            .Setup(x => x.ExecuteIdempotentAsync(evt, It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<MediaDeletedEvent, Func<CancellationToken, Task>, CancellationToken>(async (_, action, ct) =>
            {
                await action(ct);
                return true;
            });

        var poolMock = new Mock<IShufflePoolStore>();
        var handler = new MediaDeletedEventHandler(inboxMock.Object, poolMock.Object);

        await handler.Handle(evt, CancellationToken.None);

        poolMock.Verify(x => x.RemoveMediaAsync(mediaId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
