using Microsoft.Extensions.Logging;
using Quartz;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;

namespace ShuffleSeries.ShuffleEngine.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class EnsureShuffleCacheWarmJob(
    IShuffleCacheWarmingService cacheWarmingService,
    ILogger<EnsureShuffleCacheWarmJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context?.CancellationToken ?? CancellationToken.None;

        try
        {
            await cacheWarmingService.EnsureWarmedAsync(forceRebuild: false, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(
                    ex,
                    "Cold-start / periodic cache warming check encountered a transient error; next scheduled cycle will retry.");
            }
        }
    }
}
