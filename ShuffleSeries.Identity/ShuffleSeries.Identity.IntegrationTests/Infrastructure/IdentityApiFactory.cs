using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Respawn;
using ShuffleSeries.Identity.Infrastructure.BackgroundJobs;
using ShuffleSeries.Identity.Infrastructure.Persistence;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace ShuffleSeries.Identity.IntegrationTests.Infrastructure;

public class IdentityApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    static IdentityApiFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__Secret", "SuperSecretSecureKeyForIdentityIntegrationTesting2026!!");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "ShuffleSeries.Identity");
        Environment.SetEnvironmentVariable("Jwt__Audience", "ShuffleSeries.Clients");
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", "60");
        Environment.SetEnvironmentVariable("Vault__Enabled", "false");
        Environment.SetEnvironmentVariable("Vault__Optional", "true");
    }

    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("shuffleseries_identity_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management-alpine")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    private DbConnection _dbConnection = default!;
    private IConnectionMultiplexer _redisConnection = default!;
    private Respawner _respawner = default!;

    public IConnectionMultiplexer Redis => _redisConnection;

    public async Task InitializeAsync()
    {
        // 1. Gerçek PostgreSQL, Redis ve RabbitMQ container'larını paralel başlat
        await Task.WhenAll(
            _dbContainer.StartAsync(),
            _redisContainer.StartAsync(),
            _rabbitMqContainer.StartAsync());

        // 2. DbConnection'ı aç ve Respawn'ı yapılandır
        _dbConnection = new NpgsqlConnection(_dbContainer.GetConnectionString());
        await _dbConnection.OpenAsync();

        // 3. Redis bağlantısını aç (test arası blacklist temizliği için allowAdmin=true zorunludur)
        _redisConnection = await ConnectionMultiplexer.ConnectAsync(
            $"{_redisContainer.Hostname}:{_redisContainer.GetMappedPublicPort(6379)},abortConnect=false,allowAdmin=true");

        // 4. WebApplicationFactory ilk başlatıldığında EF migration'ları ve seed verileri uygulanır
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await dbContext.Database.MigrateAsync();

        // 5. Respawner nesnesini başlat (migration ve seed tablolarını sıfırlama dışında tutabiliriz)
        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = ["__EFMigrationsHistory"]
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Testcontainers yönlendirmeleri
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = _dbContainer.GetConnectionString(),
                ["Redis:Host"] = _redisContainer.Hostname,
                ["Redis:Port"] = _redisContainer.GetMappedPublicPort(6379).ToString(),
                ["Redis:AbortOnConnectFail"] = "false",
                ["MessageBroker:Host"] = _rabbitMqContainer.Hostname,
                ["MessageBroker:Port"] = _rabbitMqContainer.GetMappedPublicPort(5672).ToString(),
                ["MessageBroker:Username"] = "guest",
                ["MessageBroker:Password"] = "guest",
                ["Vault:Enabled"] = "false",
                ["Vault:Optional"] = "true",
                ["Jwt:Secret"] = "SuperSecretSecureKeyForIdentityIntegrationTesting2026!!",
                ["Jwt:Issuer"] = "ShuffleSeries.Identity",
                ["Jwt:Audience"] = "ShuffleSeries.Clients",
                ["Jwt:ExpirationMinutes"] = "60"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = context =>
                {
                    if (context.Exception is not null)
                    {
                        Console.WriteLine($"[TEST_DEBUG_EXCEPTION] {context.Exception}");
                    }
                };
            });

            // Otonom Quartz sayacını test ortamında kaldırıyoruz (Respawn ile deadlock olmaması için).
            var quartzHostedServices = services
                .Where(d => d.ServiceType == typeof(IHostedService) &&
                            d.ImplementationType?.FullName?.Contains("Quartz", StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            foreach (var service in quartzHostedServices)
            {
                services.Remove(service);
            }

            services.AddScoped<ProcessOutboxMessagesJob>();
            services.AddScoped<PurgeExpiredRefreshTokensJob>();
        });
    }

    public async Task ResetStateAsync()
    {
        // 1. PostgreSQL tablolarını truncate et
        if (_dbConnection != default!)
        {
            await _respawner.ResetAsync(_dbConnection);
        }

        // 2. Redis veritabanını temizle
        if (_redisConnection is { IsConnected: true })
        {
            var server = _redisConnection.GetServers().FirstOrDefault();
            if (server is { IsConnected: true })
            {
                await server.FlushDatabaseAsync();
            }
        }
    }

    public Task ResetDatabaseAsync() => ResetStateAsync();

    public new async Task DisposeAsync()
    {
        if (_dbConnection != default!)
        {
            await _dbConnection.CloseAsync();
            await _dbConnection.DisposeAsync();
        }

        if (_redisConnection != default!)
        {
            await _redisConnection.CloseAsync();
            await _redisConnection.DisposeAsync();
        }

        await Task.WhenAll(
            _dbContainer.StopAsync(),
            _redisContainer.StopAsync(),
            _rabbitMqContainer.StopAsync());

        await _dbContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();

        await base.DisposeAsync();
    }
}
