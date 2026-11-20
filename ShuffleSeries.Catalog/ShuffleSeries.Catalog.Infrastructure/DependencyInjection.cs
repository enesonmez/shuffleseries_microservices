using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Catalog.Infrastructure.BackgroundJobs;
using ShuffleSeries.Catalog.Infrastructure.Persistence;
using ShuffleSeries.Catalog.Infrastructure.Persistence.Repositories;
using ShuffleSeries.Catalog.Infrastructure.Services;
using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Domain.Repositories;
using ShuffleSeries.Shared.Core.Infrastructure;
using ShuffleSeries.Shared.Core.Infrastructure.Configuration.Redis;
using ShuffleSeries.Shared.Core.Infrastructure.Health;
using ShuffleSeries.Shared.Core.Infrastructure.Interceptors;
using Wolverine;
using Wolverine.HealthChecks;
using Wolverine.RabbitMQ;

namespace ShuffleSeries.Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ==========================================
        // POSTGRESQL KONFİGÜRASYONU
        // ==========================================
        services.AddSharedInfrastructure();
        services.AddDbContext<CatalogDbContext>((sp, options) =>
        {
            var auditableInterceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
            var softDeleteInterceptor = sp.GetRequiredService<SoftDeleteInterceptor>();
            var outboxInterceptor = sp.GetRequiredService<InsertOutboxMessagesInterceptor>();

            options.UseNpgsql(configuration.GetConnectionString("Database"))
                .AddInterceptors(auditableInterceptor, softDeleteInterceptor, outboxInterceptor);
        });

        // PostgreSQL veritabanı hazır bulunuşluk sağlık kontrolü
        services.AddSharedDatabaseHealthCheck<CatalogDbContext>();


        // ==========================================
        // QUARTZ BACKGROUND JOB KONFİGÜRASYONU
        // ==========================================
        services.AddQuartz(configure =>
        {
            var jobKey = new JobKey(nameof(ProcessOutboxMessagesJob));

            configure.AddJob<ProcessOutboxMessagesJob>(jobKey)
                .AddTrigger(trigger =>
                    trigger.ForJob(jobKey)
                        .WithIdentity($"{nameof(ProcessOutboxMessagesJob)}-Trigger")
                        .WithSimpleSchedule(schedule =>
                            schedule.WithIntervalInSeconds(10)
                                .RepeatForever()));
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        // Redis & Önbellek Yapılandırması (Distributed Cache - Multi-Replica Consistency)
        services.AddSharedRedis(configuration);
        services.AddSharedRedisHealthCheck();
        services.AddSingleton<ICatalogCacheService, RedisCatalogCacheService>();

        // RabbitMQ Broker & Wolverine Runtime Sağlık Kontrolleri
        services.AddSharedRabbitMqHealthCheck(configuration);
        services.AddHealthChecks()
            .AddWolverine(tags: ["messaging", "wolverine", "ready"]);

        // Interface - Somut Sınıf eşleşmeleri
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CatalogDbContext>());
        services.AddScoped<IMediaRepository, MediaRepository>();
        services.AddScoped<IMovieRepository, MovieRepository>();
        services.AddScoped<ISeriesRepository, SeriesRepository>();
        services.AddScoped<IPlatformRepository, PlatformRepository>();
        services.AddScoped<IMoodRepository, MoodRepository>();
        services.AddScoped<IGenreRepository, GenreRepository>();
        services.AddScoped<IEpisodeRepository, EpisodeRepository>();

        return services;
    }

    public static void AddCatalogMessagingTransport(this WolverineOptions opts, IConfiguration configuration)
    {
        var rabbitHost = configuration["MessageBroker:Host"] ?? "localhost";
        var rabbitPort = configuration["MessageBroker:Port"] ?? "5672";
        var rabbitUser = configuration["MessageBroker:Username"] ?? "guest";
        var rabbitPass = configuration["MessageBroker:Password"] ?? "guest";

        opts.UseRabbitMq(new Uri($"amqp://{rabbitUser}:{rabbitPass}@{rabbitHost}:{rabbitPort}"))
            .AutoProvision();

        // RabbitMQ Outbound Event Routing (Strictly IIntegrationEvent to prevent internal domain event leakage)
        opts.Publish(rule =>
        {
            rule.MessagesImplementing<IIntegrationEvent>()
                .ToRabbitExchange("shuffleseries.catalog");
        });
    }
}
