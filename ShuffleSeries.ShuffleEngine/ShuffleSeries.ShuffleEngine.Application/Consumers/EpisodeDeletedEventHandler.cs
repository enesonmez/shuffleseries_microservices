using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;

namespace ShuffleSeries.ShuffleEngine.Application.Consumers;

public sealed class EpisodeDeletedEventHandler(
    IInboxIdempotencyService inboxService,
    IShufflePoolStore poolStore)
{
    public async Task Handle(EpisodeDeletedEvent integrationEvent, CancellationToken cancellationToken)
    {
        await inboxService.ExecuteIdempotentAsync(
            integrationEvent,
            ct => poolStore.RemoveEpisodeAsync(integrationEvent.EpisodeId, ct),
            cancellationToken);
    }
}
