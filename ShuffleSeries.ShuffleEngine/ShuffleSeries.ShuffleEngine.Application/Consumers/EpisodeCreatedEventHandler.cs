using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;

namespace ShuffleSeries.ShuffleEngine.Application.Consumers;

public sealed class EpisodeCreatedEventHandler(
    IInboxIdempotencyService inboxService,
    ICatalogSyncGrpcClient catalogClient,
    IShufflePoolStore poolStore)
{
    public async Task Handle(EpisodeCreatedEvent integrationEvent, CancellationToken cancellationToken)
    {
        await inboxService.ExecuteIdempotentAsync(
            integrationEvent,
            async ct =>
            {
                if (!integrationEvent.IsStandalone)
                {
                    return;
                }

                var episode = await catalogClient.GetStandaloneEpisodeByIdAsync(integrationEvent.EpisodeId, ct);
                if (episode is not null)
                {
                    await poolStore.UpsertEpisodesBatchAsync([episode], integrationEvent.OccurredOnUtc.Ticks, ct);
                }
            },
            cancellationToken);
    }
}
