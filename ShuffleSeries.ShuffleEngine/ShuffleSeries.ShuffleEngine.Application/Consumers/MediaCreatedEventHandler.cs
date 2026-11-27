using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Domain.Enums;

namespace ShuffleSeries.ShuffleEngine.Application.Consumers;

public sealed class MediaCreatedEventHandler(
    IInboxIdempotencyService inboxService,
    ICatalogSyncGrpcClient catalogClient,
    IShufflePoolStore poolStore)
{
    public async Task Handle(MediaCreatedEvent integrationEvent, CancellationToken cancellationToken)
    {
        await inboxService.ExecuteIdempotentAsync(
            integrationEvent,
            async ct =>
            {
                var media = await catalogClient.GetMediaByIdAsync(integrationEvent.MediaId, ct);
                if (media is null)
                {
                    return;
                }

                await poolStore.UpsertMediaBatchAsync([media], integrationEvent.OccurredOnUtc.Ticks, ct);

                if (media.Kind == ShuffleMediaKind.Series)
                {
                    var episodes = await catalogClient.GetStandaloneEpisodesBySeriesIdAsync(media.Id, ct);
                    if (episodes.Count > 0)
                    {
                        await poolStore.UpsertEpisodesBatchAsync(episodes, integrationEvent.OccurredOnUtc.Ticks, ct);
                    }
                }
            },
            cancellationToken);
    }
}
