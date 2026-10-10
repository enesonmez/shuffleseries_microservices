using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using ShuffleSeries.Identity.Infrastructure.Persistence;

namespace ShuffleSeries.Identity.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class PurgeExpiredRefreshTokensJob : IJob
{
    public const int RetentionDays = 30;

    private readonly IdentityDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PurgeExpiredRefreshTokensJob> _logger;

    public PurgeExpiredRefreshTokensJob(
        IdentityDbContext dbContext,
        TimeProvider timeProvider,
        ILogger<PurgeExpiredRefreshTokensJob> logger)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public Task Execute(IJobExecutionContext context) =>
        ExecuteAsync(context.CancellationToken);

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = _timeProvider.GetUtcNow().AddDays(-RetentionDays).UtcDateTime;

        var deletedCount = await _dbContext.RefreshTokens
            .Where(rt => rt.ExpiresAtUtc < cutoff || (rt.RevokedAtUtc != null && rt.RevokedAtUtc < cutoff))
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedCount > 0 && _logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Purged {Count} expired and revoked refresh tokens older than {Cutoff:u}.", deletedCount, cutoff);
        }
    }
}
