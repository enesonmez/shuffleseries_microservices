using System.Globalization;
using Microsoft.Extensions.Options;
using ShuffleSeries.Shared.Core.Domain.Enums;
using ShuffleSeries.Shared.Core.Infrastructure.Configuration.Redis;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Application.Common.Models;
using ShuffleSeries.ShuffleEngine.Domain.Constants;
using ShuffleSeries.ShuffleEngine.Domain.Models;
using ShuffleSeries.ShuffleEngine.Infrastructure.Constants;
using StackExchange.Redis;

namespace ShuffleSeries.ShuffleEngine.Infrastructure.Services;

internal sealed class RedisUserShuffleStateStore(
    IConnectionMultiplexer redis,
    IOptions<RedisOptions> redisOptions,
    TimeProvider timeProvider) : IUserShuffleStateStore
{
    private const int MinEntitlementTtlSeconds = 86_400;        // 24 hours
    private const int MaxEntitlementTtlSeconds = 31_622_400;    // 366 days

    // Atomically updates affinity weights inside Redis while clamping each field within [minWeight, maxWeight].
    private const string UpdateAffinityLuaScript = """
        local key = KEYS[1]
        local delta = tonumber(ARGV[1])
        local minW = tonumber(ARGV[2])
        local maxW = tonumber(ARGV[3])
        local updatedAt = ARGV[4]
        local ttlSeconds = tonumber(ARGV[5])

        for i = 6, #ARGV do
            local field = ARGV[i]
            local current = tonumber(redis.call('HGET', key, field) or '0')
            local nextVal = current + delta
            if nextVal < minW then nextVal = minW end
            if nextVal > maxW then nextVal = maxW end
            redis.call('HSET', key, field, tostring(nextVal))
        end

        redis.call('HSET', key, 'updatedAtUtc', updatedAt)
        redis.call('EXPIRE', key, ttlSeconds)
        return 1
        """;

    // Guards against out-of-order subscription events via occurredAtTicks comparison.
    private const string UpsertEntitlementLuaScript = """
        local key = KEYS[1]
        local incomingTicks = tonumber(ARGV[1])
        local existingTicks = tonumber(redis.call('HGET', key, 'occurredAtTicks') or '0')
        if incomingTicks > 0 and existingTicks > incomingTicks then
            return 0
        end

        redis.call('HSET', key,
            'isPremium', ARGV[2],
            'planCode', ARGV[3],
            'expiresAtUtc', ARGV[4],
            'occurredAtTicks', ARGV[1],
            'updatedAtUtc', ARGV[5])
        redis.call('EXPIRE', key, tonumber(ARGV[6]))
        return 1
        """;

    private string Prefix => redisOptions.Value.InstanceName ?? string.Empty;

    public async Task AddToCooldownAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        var key = ShuffleRedisKeys.UserCooldownZSet(Prefix, userId);
        var nowUnix = timeProvider.GetUtcNow().ToUnixTimeSeconds();
        var expiresUnix = nowUnix + (ShuffleDomainConstants.CooldownHours * 3600);
        var ttl = ShuffleRedisKeys.WithJitter(ShuffleRedisKeys.BaseCooldownTtl, maxJitterSeconds: 120);

        var batch = db.CreateBatch();
        var evictTask = batch.SortedSetRemoveRangeByScoreAsync(key, double.NegativeInfinity, nowUnix);
        var addTask = batch.SortedSetAddAsync(key, itemId.ToString(), expiresUnix);
        var expireTask = batch.KeyExpireAsync(key, ttl);
        batch.Execute();

        await Task.WhenAll(evictTask, addTask, expireTask);
    }

    public async Task<IReadOnlySet<Guid>> GetActiveCooldownIdsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        var key = ShuffleRedisKeys.UserCooldownZSet(Prefix, userId);
        var nowUnix = timeProvider.GetUtcNow().ToUnixTimeSeconds();

        var activeMembers = await db.SortedSetRangeByScoreAsync(
            key,
            start: nowUnix,
            stop: double.PositiveInfinity,
            exclude: Exclude.Start);

        if (activeMembers.Length == 0)
        {
            return new HashSet<Guid>();
        }

        var result = new HashSet<Guid>(activeMembers.Length);
        for (var i = 0; i < activeMembers.Length; i++)
        {
            if (Guid.TryParse((string?)activeMembers[i], out var itemId))
            {
                result.Add(itemId);
            }
        }

        return result;
    }

    public async Task<UserFeatureVector> GetFeatureVectorAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var entries = await db.HashGetAllAsync(ShuffleRedisKeys.UserAffinityHash(Prefix, userId));

        if (entries.Length == 0)
        {
            return UserFeatureVector.Empty(userId, nowUtc);
        }

        var accumulator = new FeatureVectorAccumulator(nowUtc);
        for (var i = 0; i < entries.Length; i++)
        {
            accumulator.ApplyEntry(entries[i]);
        }

        return accumulator.Build(userId);
    }

    private sealed class FeatureVectorAccumulator(DateTime initialUpdatedAtUtc)
    {
        private readonly Dictionary<Guid, double> _moodWeights = [];
        private readonly Dictionary<Guid, double> _genreWeights = [];
        private readonly Dictionary<string, double> _directorWeights = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<Guid> _onboardingMoodIds = [];
        private DateTime _updatedAtUtc = initialUpdatedAtUtc;

        public void ApplyEntry(HashEntry entry)
        {
            var name = (string?)entry.Name;
            var val = (string?)entry.Value;
            if (string.IsNullOrEmpty(name) || val is null)
            {
                return;
            }

            if (TryApplyMetadataOrOnboarding(name, val))
            {
                return;
            }

            if (!double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out var weight))
            {
                return;
            }

            AssignWeightedFeature(name, weight);
        }

        private bool TryApplyMetadataOrOnboarding(string name, string val)
        {
            if (name == "updatedAtUtc")
            {
                if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedUpdated))
                {
                    _updatedAtUtc = parsedUpdated;
                }

                return true;
            }

            if (!name.StartsWith("onboarding:mood:", StringComparison.Ordinal))
            {
                return false;
            }

            if (Guid.TryParse(name.AsSpan("onboarding:mood:".Length), out var obMoodId))
            {
                _onboardingMoodIds.Add(obMoodId);
            }

            return true;
        }

        private void AssignWeightedFeature(string name, double weight)
        {
            if (name.StartsWith("mood:", StringComparison.Ordinal) &&
                Guid.TryParse(name.AsSpan("mood:".Length), out var moodId))
            {
                _moodWeights[moodId] = UserFeatureVector.ClampWeight(weight);
                return;
            }

            if (name.StartsWith("genre:", StringComparison.Ordinal) &&
                Guid.TryParse(name.AsSpan("genre:".Length), out var genreId))
            {
                _genreWeights[genreId] = UserFeatureVector.ClampWeight(weight);
                return;
            }

            if (name.StartsWith("director:", StringComparison.Ordinal))
            {
                var directorSlug = name["director:".Length..];
                if (!string.IsNullOrWhiteSpace(directorSlug))
                {
                    _directorWeights[directorSlug] = UserFeatureVector.ClampWeight(weight);
                }
            }
        }

        public UserFeatureVector Build(Guid userId) =>
            new(
                userId,
                _moodWeights,
                _genreWeights,
                _directorWeights,
                _onboardingMoodIds,
                _updatedAtUtc);
    }

    public async Task ApplyMediaRatingAffinityAsync(
        Guid userId,
        ShuffleMediaAffinityContext media,
        MediaRatingTier tier,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var delta = UserFeatureVector.GetRatingDelta(tier);
        if (Math.Abs(delta) < double.Epsilon)
        {
            return;
        }

        var targetFields = new List<string>(media.MoodIds.Count + media.GenreIds.Count + 1);
        for (var i = 0; i < media.MoodIds.Count; i++)
        {
            targetFields.Add($"mood:{media.MoodIds[i]}");
        }

        for (var i = 0; i < media.GenreIds.Count; i++)
        {
            targetFields.Add($"genre:{media.GenreIds[i]}");
        }

        if (!string.IsNullOrWhiteSpace(media.Director))
        {
            targetFields.Add($"director:{UserFeatureVector.NormalizeDirectorKey(media.Director)}");
        }

        if (targetFields.Count == 0)
        {
            return;
        }

        var nowIso = timeProvider.GetUtcNow().UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
        var ttlSeconds = (int)ShuffleRedisKeys.WithJitter(ShuffleRedisKeys.BaseAffinityTtl).TotalSeconds;

        var args = new RedisValue[5 + targetFields.Count];
        args[0] = delta.ToString(CultureInfo.InvariantCulture);
        args[1] = ShuffleDomainConstants.MinFeatureWeight.ToString(CultureInfo.InvariantCulture);
        args[2] = ShuffleDomainConstants.MaxFeatureWeight.ToString(CultureInfo.InvariantCulture);
        args[3] = nowIso;
        args[4] = ttlSeconds;

        for (var i = 0; i < targetFields.Count; i++)
        {
            args[5 + i] = targetFields[i];
        }

        var db = redis.GetDatabase();
        var key = (RedisKey)ShuffleRedisKeys.UserAffinityHash(Prefix, userId);
        await db.ScriptEvaluateAsync(UpdateAffinityLuaScript, [key], args);
    }

    public async Task ApplyOnboardingPreferencesAsync(
        Guid userId,
        IReadOnlyCollection<Guid> moodIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (moodIds.Count == 0)
        {
            return;
        }

        var db = redis.GetDatabase();
        var key = ShuffleRedisKeys.UserAffinityHash(Prefix, userId);
        var nowIso = timeProvider.GetUtcNow().UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
        var ttl = ShuffleRedisKeys.WithJitter(ShuffleRedisKeys.BaseAffinityTtl);

        var entries = new List<HashEntry>(moodIds.Count + 1)
        {
            new("updatedAtUtc", nowIso)
        };

        foreach (var moodId in moodIds)
        {
            entries.Add(new HashEntry($"onboarding:mood:{moodId}", "1"));
        }

        var batch = db.CreateBatch();
        var setTask = batch.HashSetAsync(key, entries.ToArray());
        var expTask = batch.KeyExpireAsync(key, ttl);
        batch.Execute();

        await Task.WhenAll(setTask, expTask);
    }

    public async Task<bool> SetEntitlementAsync(
        UserShuffleEntitlement entitlement,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        var key = (RedisKey)ShuffleRedisKeys.UserEntitlementHash(Prefix, entitlement.UserId);
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var baseSeconds = MinEntitlementTtlSeconds;
        if (entitlement.IsPremium && entitlement.ExpiresAtUtc.HasValue)
        {
            var remainingSeconds = (int)Math.Ceiling((entitlement.ExpiresAtUtc.Value - nowUtc).TotalSeconds);
            baseSeconds = Math.Clamp(remainingSeconds, MinEntitlementTtlSeconds, MaxEntitlementTtlSeconds);
        }

        var ttlWithJitter = baseSeconds + Random.Shared.Next(0, 301);

        RedisValue[] args =
        [
            entitlement.OccurredAtTicks,
            entitlement.IsPremium ? "1" : "0",
            entitlement.PlanCode ?? string.Empty,
            entitlement.ExpiresAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
            entitlement.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            ttlWithJitter
        ];

        var result = await db.ScriptEvaluateAsync(UpsertEntitlementLuaScript, [key], args);
        return (int)result == 1;
    }

    public async Task<UserShuffleEntitlement?> GetEntitlementAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        var values = await db.HashGetAsync(
            ShuffleRedisKeys.UserEntitlementHash(Prefix, userId),
            ["isPremium", "planCode", "expiresAtUtc", "occurredAtTicks", "updatedAtUtc"]);

        if (!values[0].HasValue)
        {
            return null;
        }

        var isPremium = (string?)values[0] == "1";
        var planCode = string.IsNullOrWhiteSpace((string?)values[1]) ? null : (string?)values[1];
        DateTime? expiresAtUtc = DateTime.TryParse(
            (string?)values[2],
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var parsedExp)
            ? parsedExp
            : null;
        var occurredAtTicks = long.TryParse(
            (string?)values[3],
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var parsedTicks)
            ? parsedTicks
            : 0L;
        var updatedAtUtc = DateTime.TryParse(
            (string?)values[4],
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var parsedUpd)
            ? parsedUpd
            : timeProvider.GetUtcNow().UtcDateTime;

        return new UserShuffleEntitlement(
            userId,
            isPremium,
            planCode,
            expiresAtUtc,
            occurredAtTicks,
            updatedAtUtc);
    }

    public async Task PurgeUserStateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        var prefix = Prefix;
        RedisKey[] keys =
        [
            ShuffleRedisKeys.UserCooldownZSet(prefix, userId),
            ShuffleRedisKeys.UserAffinityHash(prefix, userId),
            ShuffleRedisKeys.UserEntitlementHash(prefix, userId)
        ];

        await db.KeyDeleteAsync(keys);
    }
}
