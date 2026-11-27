using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;

namespace ShuffleSeries.ShuffleEngine.Application.Consumers;

public sealed class MediaDeletedEventHandler(
    IInboxIdempotencyService inboxService,
    IShufflePoolStore poolStore)
{
    public async Task Handle(MediaDeletedEvent integrationEvent, CancellationToken cancellationToken)
    {
        await inboxService.ExecuteIdempotentAsync(
            integrationEvent,
            ct => poolStore.RemoveMediaAsync(integrationEvent.MediaId, ct),
            cancellationToken);
    }
}
