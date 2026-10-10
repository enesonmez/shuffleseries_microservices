using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace ShuffleSeries.Shared.Core.Infrastructure.Health;

/// <summary>
/// StackExchange.Redis IConnectionMultiplexer üzerinden Redis bağlantı durumunu
/// ve ping gecikmesini denetleyen ASP.NET Core Health Check bileşeni.
/// </summary>
public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;

    public RedisHealthCheck(IConnectionMultiplexer redis)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_redis.IsConnected)
            {
                return new HealthCheckResult(
                    context.Registration.FailureStatus,
                    description: "Redis bağlantısı aktif değil (Not connected).");
            }

            var db = _redis.GetDatabase();
            var latency = await db.PingAsync();

            var data = new Dictionary<string, object>
            {
                ["latencyMs"] = latency.TotalMilliseconds,
                ["endpoints"] = string.Join(", ", _redis.GetEndPoints().Select(e => e.ToString()))
            };

            return HealthCheckResult.Healthy(
                $"Redis sağlıklı. Ping gecikmesi: {latency.TotalMilliseconds.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}ms",
                data);
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                description: "Redis sağlık kontrolü sırasında hata oluştu.",
                exception: ex);
        }
    }
}
