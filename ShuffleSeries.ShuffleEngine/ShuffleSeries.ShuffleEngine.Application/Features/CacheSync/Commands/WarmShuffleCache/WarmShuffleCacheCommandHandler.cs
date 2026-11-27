using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Application.Features.CacheSync.Responses;

namespace ShuffleSeries.ShuffleEngine.Application.Features.CacheSync.Commands.WarmShuffleCache;

public sealed class WarmShuffleCacheCommandHandler(IShuffleCacheWarmingService cacheWarmingService)
{
    public async Task<WarmShuffleCacheResponse> Handle(
        WarmShuffleCacheCommand request,
        CancellationToken cancellationToken) =>
        await cacheWarmingService.EnsureWarmedAsync(request.ForceRebuild, cancellationToken);
}
