using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;

namespace ShuffleSeries.ShuffleEngine.Application.Consumers;

public sealed class UserAccountDeletedEventHandler(
    IInboxIdempotencyService inboxService,
    IUserShuffleStateStore userStateStore)
{
    public async Task Handle(UserAccountDeletedEvent integrationEvent, CancellationToken cancellationToken)
    {
        await inboxService.ExecuteIdempotentAsync(
            integrationEvent,
            ct => userStateStore.PurgeUserStateAsync(integrationEvent.UserId, ct),
            cancellationToken);
    }
}
