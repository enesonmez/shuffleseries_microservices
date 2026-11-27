using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Domain.Models;

namespace ShuffleSeries.ShuffleEngine.Application.Consumers;

public sealed class UserSubscriptionCancelledEventHandler(
    IInboxIdempotencyService inboxService,
    IUserShuffleStateStore userStateStore,
    TimeProvider timeProvider)
{
    public async Task Handle(UserSubscriptionCancelledEvent integrationEvent, CancellationToken cancellationToken)
    {
        await inboxService.ExecuteIdempotentAsync(
            integrationEvent,
            ct =>
            {
                var entitlement = new UserShuffleEntitlement(
                    integrationEvent.UserId,
                    IsPremium: false,
                    PlanCode: null,
                    ExpiresAtUtc: integrationEvent.CancelledAtUtc,
                    integrationEvent.OccurredOnUtc.Ticks,
                    timeProvider.GetUtcNow().UtcDateTime);

                return userStateStore.SetEntitlementAsync(entitlement, ct);
            },
            cancellationToken);
    }
}
