using Moq;
using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Application.Consumers;
using ShuffleSeries.ShuffleEngine.Domain.Models;

namespace ShuffleSeries.ShuffleEngine.Tests.Application.Consumers;

public class UserSubscriptionCancelledEventHandlerTests
{
    [Fact]
    public async Task Handle_ShouldRevokePremiumEntitlementInRedis()
    {
        var userId = Guid.NewGuid();
        var cancelledAt = DateTime.UtcNow;
        var evt = new UserSubscriptionCancelledEvent(Guid.NewGuid(), userId, cancelledAt, cancelledAt);

        var inboxMock = new Mock<IInboxIdempotencyService>();
        inboxMock
            .Setup(x => x.ExecuteIdempotentAsync(evt, It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<UserSubscriptionCancelledEvent, Func<CancellationToken, Task>, CancellationToken>(async (_, action, ct) =>
            {
                await action(ct);
                return true;
            });

        var stateMock = new Mock<IUserShuffleStateStore>();
        var handler = new UserSubscriptionCancelledEventHandler(inboxMock.Object, stateMock.Object, TimeProvider.System);

        await handler.Handle(evt, CancellationToken.None);

        stateMock.Verify(
            x => x.SetEntitlementAsync(
                It.Is<UserShuffleEntitlement>(e => e.UserId == userId && !e.IsPremium && e.PlanCode == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
