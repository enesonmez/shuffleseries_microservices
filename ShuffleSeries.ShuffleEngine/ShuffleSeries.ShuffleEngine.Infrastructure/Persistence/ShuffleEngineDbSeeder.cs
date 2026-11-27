namespace ShuffleSeries.ShuffleEngine.Infrastructure.Persistence;

public static class ShuffleEngineDbSeeder
{
    public static Task SeedAsync(ShuffleEngineDbContext _, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
