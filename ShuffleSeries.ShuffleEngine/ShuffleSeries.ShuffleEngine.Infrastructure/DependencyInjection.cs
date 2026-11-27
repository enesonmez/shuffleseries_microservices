using JasperFx.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Domain.Repositories;
using ShuffleSeries.Shared.Core.Infrastructure;
using ShuffleSeries.Shared.Core.Infrastructure.Configuration.Redis;
using ShuffleSeries.Shared.Core.Infrastructure.Health;
using ShuffleSeries.Shared.Core.Infrastructure.Interceptors;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Infrastructure.BackgroundJobs;
using ShuffleSeries.ShuffleEngine.Infrastructure.Grpc.Catalog;
using ShuffleSeries.ShuffleEngine.Infrastructure.Persistence;
using ShuffleSeries.ShuffleEngine.Infrastructure.Services;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.HealthChecks;
using Wolverine.RabbitMQ;

namespace ShuffleSeries.ShuffleEngine.Infrastructure;

public static class DependencyInjection
{
    public const string InboundQueueName = "shuffleseries.shuffleengine.events";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSharedInfrastructure();

        services.AddDbContext<ShuffleEngineDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Database"))
                .AddInterceptors(
                    sp.GetRequiredService<AuditableEntityInterceptor>(),
                    sp.GetRequiredService<SoftDeleteInterceptor>(),
                    sp.GetRequiredService<InsertOutboxMessagesInterceptor>());
        });

        services.AddSharedDatabaseHealthCheck<ShuffleEngineDbContext>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ShuffleEngineDbContext>());
        services.AddScoped<IInboxIdempotencyService, EfInboxIdempotencyService>();

        // Redis In-Memory Pool & User State Stores
        services.AddSharedRedis(configuration);
        services.AddSharedRedisHealthCheck();
        services.AddSingleton<IShufflePoolStore, RedisShufflePoolStore>();
        services.AddSingleton<IUserShuffleStateStore, RedisUserShuffleStateStore>();

        // Internal Catalog gRPC Client (HTTP/2 Cleartext h2c + Microsoft Standard Resilience)
        var catalogGrpcAddress = configuration["CatalogGrpc:Address"] ?? "http://localhost:5012";
        services.AddGrpcClient<CatalogGrpc.CatalogGrpcClient>(options =>
            {
                options.Address = new Uri(catalogGrpcAddress);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true,
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5)
            })
            .AddStandardResilienceHandler();

        services.AddScoped<ICatalogSyncGrpcClient, CatalogSyncGrpcClient>();
        services.AddScoped<IShuffleCacheWarmingService, ShuffleCacheWarmingService>();

        // Autonomous Cold-Start & Self-Healing Cache Warming Quartz Job
        services.AddQuartz(configure =>
        {
            var warmJobKey = new JobKey(nameof(EnsureShuffleCacheWarmJob));

            configure.AddJob<EnsureShuffleCacheWarmJob>(warmJobKey)
                .AddTrigger(trigger =>
                    trigger.ForJob(warmJobKey)
                        .WithIdentity($"{nameof(EnsureShuffleCacheWarmJob)}-Trigger")
                        .StartNow()
                        .WithSimpleSchedule(schedule =>
                            schedule.WithIntervalInMinutes(5)
                                .RepeatForever()));
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        // RabbitMQ Broker & Wolverine Runtime Health Checks
        services.AddSharedRabbitMqHealthCheck(configuration);
        services.AddHealthChecks()
            .AddWolverine(tags: ["messaging", "wolverine", "ready"]);

        return services;
    }

    public static void AddShuffleEngineMessagingTransport(this WolverineOptions opts, IConfiguration configuration)
    {
        var rabbitHost = configuration["MessageBroker:Host"] ?? "localhost";
        var rabbitPort = configuration["MessageBroker:Port"] ?? "5672";
        var rabbitUser = configuration["MessageBroker:Username"] ?? "guest";
        var rabbitPass = configuration["MessageBroker:Password"] ?? "guest";

        opts.UseRabbitMq(new Uri($"amqp://{rabbitUser}:{rabbitPass}@{rabbitHost}:{rabbitPort}"))
            .AutoProvision();

        // Poison Message Resilience: MaxRetries = 3 with exponential backoff -> Dead Letter Queue
        opts.Policies.OnException<Exception>()
            .RetryWithCooldown(100.Milliseconds(), 500.Milliseconds(), 2.Seconds());

        // Inbound Event Choreography Subscriptions (Bounded Prefetch = 32 against Consumer OOM)
        opts.ListenToRabbitQueue(InboundQueueName, queue =>
            {
                queue.BindExchange("shuffleseries.catalog");
                queue.BindExchange("shuffleseries.identity");
                queue.BindExchange("shuffleseries.subscriptions");
                queue.BindExchange("shuffleseries.userlibrary");
            })
            .PreFetchCount(32);

        // Outbound Integration Event Routing (Strictly IIntegrationEvent to block domain event leakage)
        opts.Publish(rule =>
        {
            rule.MessagesImplementing<IIntegrationEvent>()
                .ToRabbitExchange("shuffleseries.shuffleengine");
        });
    }
}
