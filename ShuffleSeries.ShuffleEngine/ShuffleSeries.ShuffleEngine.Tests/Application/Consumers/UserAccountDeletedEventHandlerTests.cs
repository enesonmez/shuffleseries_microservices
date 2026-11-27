using Moq;
using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Application.Consumers;

namespace ShuffleSeries.ShuffleEngine.Tests.Application.Consumers;

public class UserAccountDeletedEventHandlerTests
{
    [Fact]
    public async Task Handle_ShouldPurgeAllUserShuffleKeysFromRedis()
    {
        var userId = Guid.NewGuid();
        var evt = new UserAccountDeletedEvent(Guid.NewGuid(), userId, null, DateTime.UtcNow);

        var inboxMock = new Mock<IInboxIdempotencyService>();
        inboxMock
            .Setup(x => x.ExecuteIdempotentAsync(evt, It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<UserAccountDeletedEvent, Func<CancellationToken, Task>, CancellationToken>(async (_, action, ct) =>
            {
                await action(ct);
                return true;
            });

        var stateMock = new Mock<IUserShuffleStateStore>();
        var handler = new UserAccountDeletedEventHandler(inboxMock.Object, stateMock.Object);

        await handler.Handle(evt, CancellationToken.None);

        stateMock.Verify(x => x.PurgeUserStateAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
