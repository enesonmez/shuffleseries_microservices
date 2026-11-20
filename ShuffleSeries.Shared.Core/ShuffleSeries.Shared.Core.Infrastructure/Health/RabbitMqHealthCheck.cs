using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace ShuffleSeries.Shared.Core.Infrastructure.Health;

/// <summary>
/// RabbitMQ broker bağlantı durumunu ve kanal yanıt süresini (latency) denetleyen ASP.NET Core Health Check bileşeni.
/// Soket tükenmesini (Socket Exhaustion) ve bağlantı izdihamını (Connection Stampede) önlemek için
/// SemaphoreSlim korumalı kalıcı IConnection havuzu kullanır.
/// </summary>
public sealed class RabbitMqHealthCheck : IHealthCheck, IAsyncDisposable, IDisposable
{
    private readonly IConnectionFactory? _connectionFactory;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly string _virtualHost;
    private readonly TimeSpan _timeout;
    private IConnection? _connection;

    public RabbitMqHealthCheck(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _host = "custom-factory";
        _port = 5672;
        _username = string.Empty;
        _password = string.Empty;
        _virtualHost = "/";
        _timeout = TimeSpan.FromSeconds(5);
    }

    public RabbitMqHealthCheck(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _host = configuration["MessageBroker:Host"] ?? "localhost";
        _port = int.TryParse(configuration["MessageBroker:Port"], out var p) ? p : 5672;
        _username = configuration["MessageBroker:Username"] ?? "guest";
        _password = configuration["MessageBroker:Password"] ?? "guest";
        _virtualHost = configuration["MessageBroker:VirtualHost"] ?? "/";
        _timeout = TimeSpan.FromSeconds(5);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var connection = await GetOrCreateConnectionAsync(cancellationToken).ConfigureAwait(false);
            if (!connection.IsOpen)
            {
                return new HealthCheckResult(
                    context.Registration.FailureStatus,
                    description: "RabbitMQ bağlantısı aktif değil (Not Open).");
            }

            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!channel.IsOpen)
            {
                return new HealthCheckResult(
                    context.Registration.FailureStatus,
                    description: "RabbitMQ kanalı oluşturulamadı veya aktif değil.");
            }

            stopwatch.Stop();
            var latencyMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2);

            var data = new Dictionary<string, object>
            {
                ["host"] = _host,
                ["port"] = _port,
                ["virtualHost"] = _virtualHost,
                ["latencyMs"] = latencyMs
            };

            return HealthCheckResult.Healthy(
                $"RabbitMQ broker bağlantısı sağlıklı. Gecikme: {latencyMs.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}ms",
                data);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                description: $"RabbitMQ broker sağlık kontrolü sırasında hata oluştu ({_host}:{_port}).",
                exception: ex);
        }
    }

    private async Task<IConnection> GetOrCreateConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync().ConfigureAwait(false);
                _connection = null;
            }

            var factory = _connectionFactory ?? new ConnectionFactory
            {
                HostName = _host,
                Port = _port,
                UserName = _username,
                Password = _password,
                VirtualHost = _virtualHost,
                RequestedConnectionTimeout = _timeout
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }

        _connectionLock.Dispose();
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _connection = null;
        _connectionLock.Dispose();
    }
}
