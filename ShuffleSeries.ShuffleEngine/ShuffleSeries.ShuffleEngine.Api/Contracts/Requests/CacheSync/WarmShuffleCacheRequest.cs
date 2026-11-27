namespace ShuffleSeries.ShuffleEngine.Api.Contracts.Requests.CacheSync;

public sealed record WarmShuffleCacheRequest(
    bool ForceRebuild = false,
    string? TriggerReason = null);
