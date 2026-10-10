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

    private const string TokenKeyPrefix = "blacklist:token:";
    private const string UserKeyPrefix = "blacklist:user:";

    public RedisTokenBlacklistService(
        IConnectionMultiplexer redis,
        IOptions<RedisOptions> options,
        ILogger<RedisTokenBlacklistService> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _options = options?.Value ?? new RedisOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            _logger.LogDebug("[RedisTokenBlacklist] Token {Jti} is already expired. Skipping Redis write.", jti);
            return;
        }

        try
        {
            var db = _redis.GetDatabase();
            var key = FormatKey(TokenKeyPrefix + jti);

            await db.StringSetAsync(key, reason, timeToLive);
            _logger.LogInformation("[RedisTokenBlacklist] Token {Jti} blacklisted for {TtlSeconds}s (Reason: {Reason})",
                jti, (int)timeToLive.TotalSeconds, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RedisTokenBlacklist] Failed to blacklist token {Jti} in Redis.", jti);
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
            _logger.LogWarning(ex, "[RedisTokenBlacklist] Redis check failed for token {Jti}. Failing open to preserve availability.", jti);
            return false;
        }
    }

    public async Task BlacklistUserTokensAsync(
        Guid userId,
        TimeSpan maxTokenLifetime,
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
            var nowUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            await db.StringSetAsync(key, nowUnixSeconds.ToString(CultureInfo.InvariantCulture), maxTokenLifetime);
            _logger.LogInformation("[RedisTokenBlacklist] All tokens for User {UserId} revoked at Unix timestamp {Timestamp} with TTL {TtlSeconds}s",
                userId, nowUnixSeconds, (int)maxTokenLifetime.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RedisTokenBlacklist] Failed to write user revocation timestamp for User {UserId} in Redis.", userId);
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

            if (long.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var revokedAtUnixSeconds))
            {
                var tokenIssuedAtSeconds = new DateTimeOffset(tokenIssuedAtUtc).ToUnixTimeSeconds();
                var isRevoked = tokenIssuedAtSeconds <= revokedAtUnixSeconds;

                if (isRevoked)
                {
                    _logger.LogWarning("[RedisTokenBlacklist] Token for User {UserId} issued at {IssuedAt} is revoked by user revocation timestamp {RevokedAt}",
                        userId, tokenIssuedAtSeconds, revokedAtUnixSeconds);
                }

                return isRevoked;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[RedisTokenBlacklist] Redis user revocation check failed for User {UserId}. Failing open to preserve availability.", userId);
            return false;
        }
    }

    private string FormatKey(string subKey)
    {
        return string.IsNullOrEmpty(_options.InstanceName)
            ? subKey
            : $"{_options.InstanceName}{subKey}";
    }
}
