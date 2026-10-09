using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using ShuffleSeries.Identity.Application.Interfaces;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Identity.Infrastructure.BackgroundJobs;
using ShuffleSeries.Identity.Infrastructure.Configuration;
using ShuffleSeries.Identity.Infrastructure.Persistence;
using ShuffleSeries.Identity.Infrastructure.Persistence.Repositories;
using ShuffleSeries.Identity.Infrastructure.Services;
using ShuffleSeries.Identity.Infrastructure.Services.Social;
using ShuffleSeries.Shared.Core.Domain.Repositories;
using ShuffleSeries.Shared.Core.Infrastructure;
using ShuffleSeries.Shared.Core.Infrastructure.Health;
using ShuffleSeries.Shared.Core.Infrastructure.Interceptors;

namespace ShuffleSeries.Identity.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSharedInfrastructure();

        services.AddDbContext<IdentityDbContext>((sp, options) =>
        {
            var auditableInterceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
            var softDeleteInterceptor = sp.GetRequiredService<SoftDeleteInterceptor>();
            var outboxInterceptor = sp.GetRequiredService<InsertOutboxMessagesInterceptor>();

            options.UseNpgsql(configuration.GetConnectionString("Database"))
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
                .AddInterceptors(auditableInterceptor, softDeleteInterceptor, outboxInterceptor);
        });

        services.AddSharedDatabaseHealthCheck<IdentityDbContext>();

        // MassTransit & RabbitMQ
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

        // Quartz Outbox Processing Job
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

        // Repositories & UnitOfWork
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();

        // Application Services
        services.AddScoped<IPermissionResolver, PermissionResolver>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        // External Social Auth Providers (Strategy / Provider Pattern)
        services.AddHttpClient();
        services.Configure<ExternalAuthOptions>(configuration.GetSection(ExternalAuthOptions.SectionName));
        services.AddScoped<ISocialAuthProvider, GoogleAuthProvider>();
        services.AddScoped<ISocialAuthProvider, AppleAuthProvider>();
        services.AddScoped<IExternalAuthService, ExternalAuthService>();
    }
}
