using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Catalog.Domain.Services;
using ShuffleSeries.Catalog.Infrastructure.BackgroundJobs;
using ShuffleSeries.Catalog.Infrastructure.Persistence;
using ShuffleSeries.Catalog.Infrastructure.Persistence.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;
using ShuffleSeries.Shared.Core.Infrastructure.Interceptors;

namespace ShuffleSeries.Catalog.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ==========================================
        // POSTGRESQL KONFİGÜRASYONU
        // ==========================================
        services.AddSingleton<InsertOutboxMessagesInterceptor>();
        services.AddDbContext<CatalogDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<InsertOutboxMessagesInterceptor>();
            
            options.UseNpgsql(configuration.GetConnectionString("Database"))
                .AddInterceptors(interceptor);
        });
        
        // ==========================================
        // MASSTRANSIT & RABBITMQ KONFİGÜRASYONU
        // ==========================================
        services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.SetKebabCaseEndpointNameFormatter();

            busConfigurator.UsingRabbitMq((context, configurator) =>
            {
                configurator.Host(configuration["MessageBroker:Host"] ?? "localhost", "/", host =>
                {
                    host.Username(configuration["MessageBroker:Username"] ?? "guest");
                    host.Password(configuration["MessageBroker:Password"] ?? "guest");
                });
                
                configurator.ConfigureEndpoints(context);
            });
        });

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

        // Interface - Somut Sınıf eşleşmeleri
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CatalogDbContext>());
        services.AddScoped<ISeriesRepository, SeriesRepository>();

        services.AddScoped<SeriesDomainService>();
    }
}