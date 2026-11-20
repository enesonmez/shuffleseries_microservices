using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShuffleSeries.Shared.Core.Application.Security;
using ShuffleSeries.Shared.Core.Infrastructure.Configuration.Redis;
using StackExchange.Redis;

namespace ShuffleSeries.Shared.Core.Infrastructure.Services.Security;

/// <summary>
/// StackExchange.Redis tabanlı, yüksek performanslı merkezi JWT Token Blacklist servisi.
/// </summary>
public sealed class RedisTokenBlacklistService : ITokenBlacklistService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly RedisOptions _options;
    private readonly ILogger<RedisTokenBlacklistService> _logger;
    private readonly TimeProvider _timeProvider;

    private const string TokenKeyPrefix = "blacklist:token:";
    private const string UserKeyPrefix = "blacklist:user:";

    public RedisTokenBlacklistService(
        IConnectionMultiplexer redis,
        IOptions<RedisOptions> options,
        ILogger<RedisTokenBlacklistService> logger,
        TimeProvider? timeProvider = null)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _options = options?.Value ?? new RedisOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task BlacklistTokenAsync(
        string jti,
        TimeSpan timeToLive,
        string reason = "revoked",
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jti);

        if (timeToLive <= TimeSpan.Zero)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("[RedisTokenBlacklist] Token {Jti} is already expired. Skipping Redis write.", jti);
            }
            return;
        }

        try
        {
            var db = _redis.GetDatabase();
            var key = FormatKey(TokenKeyPrefix + jti);

            await db.StringSetAsync(key, reason, timeToLive);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                var ttlSeconds = (int)timeToLive.TotalSeconds;
                _logger.LogInformation("[RedisTokenBlacklist] Token {Jti} blacklisted for {TtlSeconds}s (Reason: {Reason})",
                    jti, ttlSeconds, reason);
            }
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "[RedisTokenBlacklist] Failed to blacklist token {Jti} in Redis.", jti);
            }
            throw;
        }
    }

    public async Task<bool> IsTokenBlacklistedAsync(string jti, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jti);

        try
        {
            var db = _redis.GetDatabase();
            var key = FormatKey(TokenKeyPrefix + jti);

            return await db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(ex, "[RedisTokenBlacklist] Redis check failed for token {Jti}. Failing open to preserve availability.", jti);
            }
            return false;
        }
    }

    public Task BlacklistUserTokensAsync(
        Guid userId,
        TimeSpan maxTokenLifetime,
        CancellationToken cancellationToken = default) =>
        BlacklistUserTokensAsync(userId, maxTokenLifetime, revokedAt: null, cancellationToken);

    public async Task BlacklistUserTokensAsync(
        Guid userId,
        TimeSpan maxTokenLifetime,
        DateTimeOffset? revokedAt,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return;
        }

        if (maxTokenLifetime <= TimeSpan.Zero)
        {
            maxTokenLifetime = TimeSpan.FromHours(1);
        }

        try
        {
            var db = _redis.GetDatabase();
            var key = FormatKey(UserKeyPrefix + userId.ToString("N"));
            var nowUnixMs = (revokedAt ?? _timeProvider.GetUtcNow()).ToUnixTimeMilliseconds();

            await db.StringSetAsync(key, nowUnixMs.ToString(CultureInfo.InvariantCulture), maxTokenLifetime);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                var ttlSeconds = (int)maxTokenLifetime.TotalSeconds;
                _logger.LogInformation("[RedisTokenBlacklist] All tokens for User {UserId} revoked at Unix timestamp (ms) {Timestamp} with TTL {TtlSeconds}s",
                    userId, nowUnixMs, ttlSeconds);
            }
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "[RedisTokenBlacklist] Failed to write user revocation timestamp for User {UserId} in Redis.", userId);
            }
            throw;
        }
    }

    public async Task<bool> IsUserBlacklistedAsync(
        Guid userId,
        DateTime tokenIssuedAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return false;
        }

        try
        {
            var db = _redis.GetDatabase();
            var key = FormatKey(UserKeyPrefix + userId.ToString("N"));

            var value = await db.StringGetAsync(key);
            if (!value.HasValue)
            {
                return false;
            }

            if (long.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var revokedAtRaw))
            {
                return EvaluateUserRevocationTimestamp(userId, revokedAtRaw, tokenIssuedAtUtc);
            }

            return false;
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(ex, "[RedisTokenBlacklist] Redis user revocation check failed for User {UserId}. Failing open to preserve availability.", userId);
            }
            return false;
        }
    }

    public async Task<bool> IsSessionRevokedAsync(
        string? jti,
        Guid? userId,
        DateTime? tokenIssuedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var hasJti = !string.IsNullOrWhiteSpace(jti);
        var hasUser = userId.HasValue && userId.Value != Guid.Empty && tokenIssuedAtUtc.HasValue;

        if (!hasJti && !hasUser)
        {
            return false;
        }

        try
        {
            if (hasJti && hasUser)
            {
                return await CheckDualRevocationAsync(jti!, userId!.Value, tokenIssuedAtUtc!.Value);
            }

            return hasJti
                ? await IsTokenBlacklistedAsync(jti!, cancellationToken)
                : await IsUserBlacklistedAsync(userId!.Value, tokenIssuedAtUtc!.Value, cancellationToken);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(ex, "[RedisTokenBlacklist] Session revocation check failed for JTI {Jti} / User {UserId}. Failing open to preserve availability.", jti, userId);
            }
            return false;
        }
    }

    private async Task<bool> CheckDualRevocationAsync(string jti, Guid userId, DateTime tokenIssuedAtUtc)
    {
        var db = _redis.GetDatabase();
        var tokenKey = (RedisKey)FormatKey(TokenKeyPrefix + jti);
        var userKey = (RedisKey)FormatKey(UserKeyPrefix + userId.ToString("N"));

        var values = await db.StringGetAsync([tokenKey, userKey]);

        if (values[0].HasValue)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("[RedisTokenBlacklist] Token {Jti} is individually blacklisted.", jti);
            }
            return true;
        }

        if (values[1].HasValue && long.TryParse(values[1].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var revokedAtRaw))
        {
            return EvaluateUserRevocationTimestamp(userId, revokedAtRaw, tokenIssuedAtUtc);
        }

        return false;
    }

    private bool EvaluateUserRevocationTimestamp(Guid userId, long revokedAtRaw, DateTime tokenIssuedAtUtc)
    {
        var revokedAtMs = revokedAtRaw > 100_000_000_000L ? revokedAtRaw : revokedAtRaw * 1000;
        var tokenIssuedAtMs = new DateTimeOffset(tokenIssuedAtUtc).ToUnixTimeMilliseconds();
        var isRevoked = tokenIssuedAtMs <= revokedAtMs;

        if (isRevoked && _logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning("[RedisTokenBlacklist] Token for User {UserId} issued at {IssuedAtMs}ms is revoked by user revocation timestamp {RevokedAtMs}ms",
                userId, tokenIssuedAtMs, revokedAtMs);
        }

        return isRevoked;
    }

    private string FormatKey(string subKey)
    {
        return string.IsNullOrEmpty(_options.InstanceName)
            ? subKey
            : $"{_options.InstanceName}{subKey}";
    }
}
