using Moq;
using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Application.Consumers;
using ShuffleSeries.ShuffleEngine.Domain.Models;

namespace ShuffleSeries.ShuffleEngine.Tests.Application.Consumers;

public class UserSubscribedEventHandlerTests
{
    [Fact]
    public async Task Handle_ShouldGrantPremiumEntitlementInRedis()
    {
        var userId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddDays(30);
        var evt = new UserSubscribedEvent(Guid.NewGuid(), userId, "VIP_MONTHLY", expiresAt, DateTime.UtcNow);

        var inboxMock = new Mock<IInboxIdempotencyService>();
        inboxMock
            .Setup(x => x.ExecuteIdempotentAsync(evt, It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<UserSubscribedEvent, Func<CancellationToken, Task>, CancellationToken>(async (_, action, ct) =>
            {
                await action(ct);
                return true;
            });

        var stateMock = new Mock<IUserShuffleStateStore>();
        var handler = new UserSubscribedEventHandler(inboxMock.Object, stateMock.Object, TimeProvider.System);

        await handler.Handle(evt, CancellationToken.None);

        stateMock.Verify(
            x => x.SetEntitlementAsync(
                It.Is<UserShuffleEntitlement>(e => e.UserId == userId && e.IsPremium && e.PlanCode == "VIP_MONTHLY" && e.ExpiresAtUtc == expiresAt),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
