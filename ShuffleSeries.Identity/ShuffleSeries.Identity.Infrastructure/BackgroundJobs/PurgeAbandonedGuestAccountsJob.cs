using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using ShuffleSeries.Identity.Infrastructure.Persistence;

namespace ShuffleSeries.Identity.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class PurgeAbandonedGuestAccountsJob : IJob
{
    public const int InactiveDaysThreshold = 60;

    private readonly IdentityDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PurgeAbandonedGuestAccountsJob> _logger;

    public PurgeAbandonedGuestAccountsJob(
        IdentityDbContext dbContext,
        TimeProvider timeProvider,
        ILogger<PurgeAbandonedGuestAccountsJob> logger)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public Task Execute(IJobExecutionContext context) =>
        ExecuteAsync(context.CancellationToken);

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = _timeProvider.GetUtcNow().AddDays(-InactiveDaysThreshold).UtcDateTime;

        var deletedCount = await _dbContext.Users
            .Where(u => u.IsGuest && u.CreatedAtUtc < cutoff && !u.RefreshTokens.Any(rt => rt.ExpiresAtUtc > cutoff))
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedCount > 0 && _logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Purged {Count} abandoned guest accounts older than {Cutoff:u}.", deletedCount, cutoff);
        }
    }
}
