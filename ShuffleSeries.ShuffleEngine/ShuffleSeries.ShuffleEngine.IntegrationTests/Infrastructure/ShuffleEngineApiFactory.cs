using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Respawn;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Infrastructure.BackgroundJobs;
using ShuffleSeries.ShuffleEngine.Infrastructure.Persistence;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace ShuffleSeries.ShuffleEngine.IntegrationTests.Infrastructure;

public class ShuffleEngineApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    static ShuffleEngineApiFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__Secret", TestJwtGenerator.TestSecret);
        Environment.SetEnvironmentVariable("Jwt__Issuer", TestJwtGenerator.TestIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", TestJwtGenerator.TestAudience);
        Environment.SetEnvironmentVariable("Vault__Enabled", "false");
        Environment.SetEnvironmentVariable("Vault__Optional", "true");
    }

    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("shuffleseries_shuffle_test")
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
    private ConnectionMultiplexer _redisConnection = default!;
    private Respawner _respawner = default!;

    public FakeCatalogSyncGrpcClient FakeCatalogClient { get; } = new();

    public IConnectionMultiplexer Redis => _redisConnection;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _dbContainer.StartAsync(),
            _redisContainer.StartAsync(),
            _rabbitMqContainer.StartAsync());

        _dbConnection = new NpgsqlConnection(_dbContainer.GetConnectionString());
        await _dbConnection.OpenAsync();

        _redisConnection = await ConnectionMultiplexer.ConnectAsync(
            $"{_redisContainer.Hostname}:{_redisContainer.GetMappedPublicPort(6379)},abortConnect=false,allowAdmin=true");

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShuffleEngineDbContext>();
        await dbContext.Database.MigrateAsync();
        await ShuffleEngineDbSeeder.SeedAsync(dbContext);

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

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = _dbContainer.GetConnectionString(),
                ["Redis:Host"] = _redisContainer.Hostname,
                ["Redis:Port"] = _redisContainer.GetMappedPublicPort(6379).ToString(),
                ["Redis:InstanceName"] = "shuffleseries:",
                ["Redis:AbortOnConnectFail"] = "false",
                ["MessageBroker:Host"] = _rabbitMqContainer.Hostname,
                ["MessageBroker:Port"] = _rabbitMqContainer.GetMappedPublicPort(5672).ToString(),
                ["MessageBroker:Username"] = "guest",
                ["MessageBroker:Password"] = "guest",
                ["Vault:Enabled"] = "false",
                ["Vault:Optional"] = "true",
                ["Jwt:Secret"] = TestJwtGenerator.TestSecret,
                ["Jwt:Issuer"] = TestJwtGenerator.TestIssuer,
                ["Jwt:Audience"] = TestJwtGenerator.TestAudience
            });
        });

        builder.ConfigureServices(services =>
        {
            // Disable autonomous Quartz background timer to prevent Respawn TRUNCATE deadlocks
            var quartzHostedServices = services
                .Where(d => d.ServiceType == typeof(IHostedService) &&
                            d.ImplementationType?.FullName?.Contains("Quartz", StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            foreach (var service in quartzHostedServices)
            {
                services.Remove(service);
            }

            services.AddScoped<EnsureShuffleCacheWarmJob>();
            services.RemoveAll<ICatalogSyncGrpcClient>();
            services.AddSingleton<ICatalogSyncGrpcClient>(FakeCatalogClient);
        });
    }

    public async Task ResetStateAsync()
    {
        FakeCatalogClient.ResetToSeedCatalog();

        if (_dbConnection != default!)
        {
            await _respawner.ResetAsync(_dbConnection);
        }

        if (_redisConnection is { IsConnected: true })
        {
            var server = _redisConnection.GetServers().FirstOrDefault();
            if (server is { IsConnected: true })
            {
                await server.FlushDatabaseAsync();
            }
        }
    }

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
