using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ShuffleSeries.Shared.Core.Infrastructure.Extensions;

/// <summary>
/// Provides resilient database migration and optional seeding extensions for EF Core DbContexts.
/// Implements exponential / linear retry loops to gracefully handle container initialization delays (e.g., PostgreSQL in Docker).
/// </summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations and executes an optional seeder callback using an <see cref="IHost"/> instance.
    /// </summary>
    public static Task ApplyDatabaseMigrationsAsync<TDbContext>(
        this IHost host,
        Func<TDbContext, IServiceProvider, Task>? seeder = null,
        int maxRetries = 6,
        int delaySeconds = 5,
        CancellationToken cancellationToken = default) where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(host);
        return ApplyDatabaseMigrationsAsync(host.Services, seeder, maxRetries, delaySeconds, cancellationToken);
    }

    /// <summary>
    /// Applies pending EF Core migrations and executes an optional seeder callback using an <see cref="IServiceProvider"/> instance.
    /// </summary>
    public static async Task ApplyDatabaseMigrationsAsync<TDbContext>(
        this IServiceProvider serviceProvider,
        Func<TDbContext, IServiceProvider, Task>? seeder = null,
        int maxRetries = 6,
        int delaySeconds = 5,
        CancellationToken cancellationToken = default) where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        using var scope = serviceProvider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TDbContext>>();

        var retryCount = 0;
        var dbContextName = typeof(TDbContext).Name;

        while (retryCount < maxRetries)
        {
            try
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Checking and applying database migrations for {DbContext}... (Attempt {Attempt}/{Max})",
                        dbContextName, retryCount + 1, maxRetries);
                }

                await context.Database.MigrateAsync(cancellationToken);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Database migrations applied successfully for {DbContext}.", dbContextName);
                }

                if (seeder is not null)
                {
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation("Applying database seed data for {DbContext}...", dbContextName);
                    }

                    await seeder(context, scope.ServiceProvider);

                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation("Database seed data applied successfully for {DbContext}.", dbContextName);
                    }
                }

                break;
            }
            catch (Exception ex)
            {
                retryCount++;
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning(ex,
                        "Database {DbContext} is not ready yet or is not accepting connections. Retrying in {Delay}s... (Attempt {Attempt}/{Max})",
                        dbContextName, delaySeconds, retryCount, maxRetries);
                }

                if (retryCount >= maxRetries)
                {
                    logger.LogError(ex,
                        "Maximum retry limit reached ({Max}). Database migrations could not be applied for {DbContext}. Terminating application.",
                        maxRetries, dbContextName);
                    throw;
                }

                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
            }
        }
    }
}
