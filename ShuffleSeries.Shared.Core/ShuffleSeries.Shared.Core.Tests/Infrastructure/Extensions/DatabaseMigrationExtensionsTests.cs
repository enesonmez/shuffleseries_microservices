using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using ShuffleSeries.Shared.Core.Infrastructure.Extensions;

namespace ShuffleSeries.Shared.Core.Tests.Infrastructure.Extensions;

public class DatabaseMigrationExtensionsTests
{
    private sealed class DummyMigrationDbContext(DbContextOptions<DummyMigrationDbContext> options)
        : DbContext(options);

    [Fact]
    public async Task ApplyDatabaseMigrationsAsync_WhenHostIsNull_ShouldThrowArgumentNullException()
    {
        IHost host = null!;

        var act = async () => await host.ApplyDatabaseMigrationsAsync<DummyMigrationDbContext>();

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("host");
    }

    [Fact]
    public async Task ApplyDatabaseMigrationsAsync_WhenServiceProviderIsNull_ShouldThrowArgumentNullException()
    {
        IServiceProvider serviceProvider = null!;

        var act = async () => await serviceProvider.ApplyDatabaseMigrationsAsync<DummyMigrationDbContext>();

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("serviceProvider");
    }

    [Fact]
    public async Task ApplyDatabaseMigrationsAsync_WhenMigrationFailsRepeatedly_ShouldRetryAndRethrowAfterMaxRetries()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace));
        services.AddDbContext<DummyMigrationDbContext>(opts =>
            opts.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        await using var serviceProvider = services.BuildServiceProvider();
        var hostMock = new Mock<IHost>();
        hostMock.Setup(h => h.Services).Returns(serviceProvider);

        var act = async () => await hostMock.Object.ApplyDatabaseMigrationsAsync<DummyMigrationDbContext>(
            seeder: (_, _) => Task.CompletedTask,
            maxRetries: 2,
            delaySeconds: 0);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
