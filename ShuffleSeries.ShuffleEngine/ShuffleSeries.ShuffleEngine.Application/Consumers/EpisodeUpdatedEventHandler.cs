using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;

namespace ShuffleSeries.ShuffleEngine.Application.Consumers;

public sealed class EpisodeUpdatedEventHandler(
    IInboxIdempotencyService inboxService,
    ICatalogSyncGrpcClient catalogClient,
    IShufflePoolStore poolStore)
{
    public async Task Handle(EpisodeUpdatedEvent integrationEvent, CancellationToken cancellationToken)
    {
        await inboxService.ExecuteIdempotentAsync(
            integrationEvent,
            async ct =>
            {
                if (!integrationEvent.IsStandalone)
                {
                    await poolStore.RemoveEpisodeAsync(integrationEvent.EpisodeId, ct);
                    return;
                }

                var episode = await catalogClient.GetStandaloneEpisodeByIdAsync(integrationEvent.EpisodeId, ct);
                if (episode is null)
                {
                    await poolStore.RemoveEpisodeAsync(integrationEvent.EpisodeId, ct);
                    return;
                }

                await poolStore.UpsertEpisodesBatchAsync([episode], integrationEvent.OccurredOnUtc.Ticks, ct);
            },
            cancellationToken);
    }
}
