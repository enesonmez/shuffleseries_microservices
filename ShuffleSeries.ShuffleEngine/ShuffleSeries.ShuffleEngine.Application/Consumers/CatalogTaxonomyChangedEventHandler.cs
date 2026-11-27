using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;

namespace ShuffleSeries.ShuffleEngine.Application.Consumers;

public sealed class CatalogTaxonomyChangedEventHandler(
    IInboxIdempotencyService inboxService,
    IShufflePoolStore poolStore,
    IShuffleCacheWarmingService warmingService)
{
    public async Task Handle(CatalogTaxonomyChangedEvent integrationEvent, CancellationToken cancellationToken)
    {
        await inboxService.ExecuteIdempotentAsync(
            integrationEvent,
            async ct =>
            {
                if (string.Equals(integrationEvent.Action, "Deleted", StringComparison.OrdinalIgnoreCase))
                {
                    await poolStore.RemoveTaxonomyIndexAsync(integrationEvent.TaxonomyType, integrationEvent.ItemId, ct);
                    return;
                }

                var isPlatformOrMood =
                    string.Equals(integrationEvent.TaxonomyType, "Platform", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(integrationEvent.TaxonomyType, "Mood", StringComparison.OrdinalIgnoreCase);

                if (isPlatformOrMood &&
                    string.Equals(integrationEvent.Action, "Updated", StringComparison.OrdinalIgnoreCase))
                {
                    await warmingService.EnsureWarmedAsync(forceRebuild: true, ct);
                }
            },
            cancellationToken);
    }
}
