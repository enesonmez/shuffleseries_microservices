namespace ShuffleSeries.ShuffleEngine.Application.Features.CacheSync.Commands.WarmShuffleCache;

public sealed record WarmShuffleCacheCommand(bool ForceRebuild = false, string? TriggerReason = null);
