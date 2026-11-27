using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Application.Common.Models;

namespace ShuffleSeries.ShuffleEngine.Application.Consumers;

public sealed class MediaRatedEventHandler(
    IInboxIdempotencyService inboxService,
    IShufflePoolStore poolStore,
    ICatalogSyncGrpcClient catalogClient,
    IUserShuffleStateStore userStateStore)
{
    public async Task Handle(MediaRatedEvent integrationEvent, CancellationToken cancellationToken)
    {
        await inboxService.ExecuteIdempotentAsync(
            integrationEvent,
            async ct =>
            {
                var context = await poolStore.GetMediaAffinityContextAsync(integrationEvent.MediaId, ct);
                if (context is null)
                {
                    var fetched = await catalogClient.GetMediaByIdAsync(integrationEvent.MediaId, ct);
                    if (fetched is null)
                    {
                        return;
                    }

                    await poolStore.UpsertMediaBatchAsync([fetched], integrationEvent.OccurredOnUtc.Ticks, ct);
                    context = new ShuffleMediaAffinityContext(
                        fetched.Id,
                        fetched.MoodIds,
                        fetched.GenreIds,
                        fetched.Director);
                }

                await userStateStore.ApplyMediaRatingAffinityAsync(
                    integrationEvent.UserId,
                    context,
                    integrationEvent.RatingTier,
                    ct);
            },
            cancellationToken);
    }
}
