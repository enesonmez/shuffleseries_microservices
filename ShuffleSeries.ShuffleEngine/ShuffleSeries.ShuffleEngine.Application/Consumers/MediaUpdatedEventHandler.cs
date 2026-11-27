using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Domain.Enums;

namespace ShuffleSeries.ShuffleEngine.Application.Consumers;

public sealed class MediaUpdatedEventHandler(
    IInboxIdempotencyService inboxService,
    ICatalogSyncGrpcClient catalogClient,
    IShufflePoolStore poolStore)
{
    public async Task Handle(MediaUpdatedEvent integrationEvent, CancellationToken cancellationToken)
    {
        await inboxService.ExecuteIdempotentAsync(
            integrationEvent,
            async ct =>
            {
                var media = await catalogClient.GetMediaByIdAsync(integrationEvent.MediaId, ct);
                if (media is null)
                {
                    await poolStore.RemoveMediaAsync(integrationEvent.MediaId, ct);
                    return;
                }

                await poolStore.UpsertMediaBatchAsync([media], integrationEvent.OccurredOnUtc.Ticks, ct);

                if (media.Kind == ShuffleMediaKind.Series)
                {
                    var episodes = await catalogClient.GetStandaloneEpisodesBySeriesIdAsync(media.Id, ct);
                    await poolStore.ReconcileSeriesEpisodesAsync(media.Id, episodes, integrationEvent.OccurredOnUtc.Ticks, ct);
                }
            },
            cancellationToken);
    }
}
