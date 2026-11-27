using ShuffleSeries.ShuffleEngine.Application.Features.CacheSync.Responses;

namespace ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;

public interface IShuffleCacheWarmingService
{
    Task<WarmShuffleCacheResponse> EnsureWarmedAsync(
        bool forceRebuild = false,
        CancellationToken cancellationToken = default);
}
