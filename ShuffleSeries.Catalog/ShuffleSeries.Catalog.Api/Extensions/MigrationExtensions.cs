using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Infrastructure.Persistence;

namespace ShuffleSeries.Catalog.Api.Extensions;

public static class MigrationExtensions
{
    public static async Task ApplyMigrationsAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<CatalogDbContext>>();

        // Docker ortamında Postgres'in tamamen hazır olmasını beklemek için akıllı yeniden deneme (Retry) mekanizması
        var retryCount = 0;
        const int maxRetries = 6;
        const int delaySeconds = 5;

        while (retryCount < maxRetries)
        {
            try
            {
                logger.LogInformation("Checking and applying database migrations... (Attempt {Attempt}/{Max})",
                    retryCount + 1, maxRetries);

                await context.Database.MigrateAsync();

                logger.LogInformation("Database migrations applied successfully. Database is ready.");
                break;
            }
            catch (Exception ex)
            {
                retryCount++;
                logger.LogWarning(ex,
                    "Database is not accepting connections yet or is not ready. Retrying in {Delay} seconds...",
                    delaySeconds);

                if (retryCount >= maxRetries)
                {
                    logger.LogError(ex,
                        "Maximum retry limit reached. Database migrations could not be applied. Terminating application.");
                    throw;
                }

                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        }
    }
}