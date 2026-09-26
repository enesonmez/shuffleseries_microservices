using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Infrastructure.Persistence;
using ShuffleSeries.Catalog.Infrastructure.Persistence.Repositories;
using ShuffleSeries.Shared.Core.Infrastructure.Interceptors;

namespace ShuffleSeries.Catalog.Tests.Infrastructure.Persistence;

public class CatalogDbContextSoftDeleteTests
{
    private static CatalogDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(new SoftDeleteInterceptor(), new InsertOutboxMessagesInterceptor())
            .Options;

        return new CatalogDbContext(options);
    }

    [Fact]
    public async Task DeleteSeries_Should_SetIsDeletedTrue_AndAuditFields()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var repository = new SeriesRepository(context);

        var series = Series.Create("Dark", "A mind-bending sci-fi thriller.", false);
        repository.Add(series);
        await context.SaveChangesAsync();

        // Act
        series.Delete("admin");
        repository.Delete(series);
        await context.SaveChangesAsync();

        // Assert - entity in DB should have IsDeleted = true
        var rawSeries = await context.Series.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == series.Id);
        rawSeries.Should().NotBeNull();
        rawSeries!.IsDeleted.Should().BeTrue();
        rawSeries.DeletedAtUtc.Should().NotBeNull();
        rawSeries.DeletedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        rawSeries.DeletedBy.Should().Be("admin");
    }

    [Fact]
    public async Task GetByIdAsync_Should_ReturnNull_When_SeriesIsSoftDeleted()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var repository = new SeriesRepository(context);

        var series = Series.Create("Stranger Things", "Hawkins mysteries.", false);
        repository.Add(series);
        await context.SaveChangesAsync();

        // Soft delete
        repository.Delete(series);
        await context.SaveChangesAsync();

        // Act - repository query using EF Core standard pipeline (with Global Query Filter)
        var retrievedSeries = await repository.GetByIdAsync(series.Id, CancellationToken.None);

        // Assert
        retrievedSeries.Should().BeNull();
    }

    [Fact]
    public async Task GetPagedListAsync_Should_ExcludeSoftDeletedSeries()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var repository = new SeriesRepository(context);

        var activeSeries1 = Series.Create("Breaking Bad", "Chemistry teacher.", false);
        var activeSeries2 = Series.Create("Better Call Saul", "Lawyer journey.", false);
        var deletedSeries = Series.Create("Old Show", "Cancelled show.", false);

        repository.Add(activeSeries1);
        repository.Add(activeSeries2);
        repository.Add(deletedSeries);
        await context.SaveChangesAsync();

        // Soft delete the third one
        repository.Delete(deletedSeries);
        await context.SaveChangesAsync();

        // Act
        var (items, totalCount) = await repository.GetPagedListAsync(1, 10, CancellationToken.None);

        // Assert
        totalCount.Should().Be(2);
        items.Should().HaveCount(2);
        items.Should().NotContain(x => x.Id == deletedSeries.Id);
    }

    [Fact]
    public async Task HardDeleteScope_Should_PhysicallyDeleteSeriesFromDatabase()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var repository = new SeriesRepository(context);

        var series = Series.Create("Temporary Show", "To be permanently purged.", false);
        repository.Add(series);
        await context.SaveChangesAsync();

        // Act - execute within HardDeleteScope
        using (ShuffleSeries.Shared.Core.Infrastructure.HardDeleteScope.Begin())
        {
            repository.Delete(series);
            await context.SaveChangesAsync();
        }

        // Assert - series must be completely deleted from PostgreSQL/DB
        var rawSeries = await context.Series.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == series.Id);
        rawSeries.Should().BeNull();
    }

    [Fact]
    public async Task Series_HardDelete_Should_PhysicallyDeleteSpecificSeries()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var repository = new SeriesRepository(context);

        var series = Series.Create("GDPR Account Data", "Must be physically wiped.", false);
        repository.Add(series);
        await context.SaveChangesAsync();

        // Act
        series.HardDelete(); // explicitly flag domain entity for hard delete
        repository.Delete(series);
        await context.SaveChangesAsync();

        // Assert
        var rawSeries = await context.Series.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == series.Id);
        rawSeries.Should().BeNull();
    }
}
