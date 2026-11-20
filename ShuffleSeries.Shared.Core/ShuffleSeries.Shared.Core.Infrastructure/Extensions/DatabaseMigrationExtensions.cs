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
        var execution = new MigrationExecutionContext<TDbContext>(
            context,
            scope.ServiceProvider,
            logger,
            seeder,
            typeof(TDbContext).Name);

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            var succeeded = await TryExecuteMigrationAttemptAsync(
                execution, attempt, maxRetries, delaySeconds, cancellationToken);

            if (succeeded)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
        }
    }

    private static async Task<bool> TryExecuteMigrationAttemptAsync<TDbContext>(
        MigrationExecutionContext<TDbContext> execution,
        int attempt,
        int maxRetries,
        int delaySeconds,
        CancellationToken cancellationToken) where TDbContext : DbContext
    {
        try
        {
            if (execution.Logger.IsEnabled(LogLevel.Information))
            {
                execution.Logger.LogInformation("Checking and applying database migrations for {DbContext}... (Attempt {Attempt}/{Max})",
                    execution.DbContextName, attempt, maxRetries);
            }

            await execution.Context.Database.MigrateAsync(cancellationToken);

            if (execution.Logger.IsEnabled(LogLevel.Information))
            {
                execution.Logger.LogInformation("Database migrations applied successfully for {DbContext}.", execution.DbContextName);
            }

            await RunSeederIfConfiguredAsync(execution);
            return true;
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            if (execution.Logger.IsEnabled(LogLevel.Warning))
            {
                execution.Logger.LogWarning(ex,
                    "Database {DbContext} is not ready yet or is not accepting connections. Retrying in {Delay}s... (Attempt {Attempt}/{Max})",
                    execution.DbContextName, delaySeconds, attempt, maxRetries);
            }
            return false;
        }
        catch (Exception ex)
        {
            if (execution.Logger.IsEnabled(LogLevel.Error))
            {
                execution.Logger.LogError(ex,
                    "Maximum retry limit reached ({Max}). Database migrations could not be applied for {DbContext}. Terminating application.",
                    maxRetries, execution.DbContextName);
            }
            throw;
        }
    }

    private static async Task RunSeederIfConfiguredAsync<TDbContext>(
        MigrationExecutionContext<TDbContext> execution) where TDbContext : DbContext
    {
        if (execution.Seeder is null)
        {
            return;
        }

        if (execution.Logger.IsEnabled(LogLevel.Information))
        {
            execution.Logger.LogInformation("Applying database seed data for {DbContext}...", execution.DbContextName);
        }

        await execution.Seeder(execution.Context, execution.ScopedServices);

        if (execution.Logger.IsEnabled(LogLevel.Information))
        {
            execution.Logger.LogInformation("Database seed data applied successfully for {DbContext}.", execution.DbContextName);
        }
    }

    private sealed record MigrationExecutionContext<TDbContext>(
        TDbContext Context,
        IServiceProvider ScopedServices,
        ILogger Logger,
        Func<TDbContext, IServiceProvider, Task>? Seeder,
        string DbContextName) where TDbContext : DbContext;
}
