using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Infrastructure.Persistence;
using ShuffleSeries.Catalog.Infrastructure.Persistence.Repositories;
using ShuffleSeries.Shared.Core.Infrastructure.Interceptors;

namespace ShuffleSeries.Catalog.Tests.Infrastructure.Persistence;

public class SeriesRepositoryTests
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
    public async Task Add_And_GetByIdAsync_ShouldPersistAndRetrieveSeries()
    {
        await using var context = CreateInMemoryDbContext();
        var repository = new SeriesRepository(context);
        var series = Series.Create("Stranger Things", "Hawkins sci-fi mystery", false);

        repository.Add(series);
        await context.SaveChangesAsync();

        var retrieved = await repository.GetByIdAsync(series.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Title.Should().Be("Stranger Things");
    }

    [Fact]
    public async Task Update_ShouldModifySeriesDetails()
    {
        await using var context = CreateInMemoryDbContext();
        var repository = new SeriesRepository(context);
        var series = Series.Create("Arcane", "Steampunk animation", false);

        repository.Add(series);
        await context.SaveChangesAsync();

        series.Update("Arcane: Season 2", "Updated description", true);
        repository.Update(series);
        await context.SaveChangesAsync();

        var updated = await repository.GetByIdAsync(series.Id);
        updated.Should().NotBeNull();
        updated!.Title.Should().Be("Arcane: Season 2");
        updated.IsIndependentEpisodes.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByTitleAsync_ShouldReturnTrueIfTitleMatches_CaseInsensitive()
    {
        await using var context = CreateInMemoryDbContext();
        var repository = new SeriesRepository(context);
        var series = Series.Create("Chernobyl", "Nuclear disaster mini-series", false);

        repository.Add(series);
        await context.SaveChangesAsync();

        var exists = await repository.ExistsByTitleAsync("chernobyl");
        exists.Should().BeTrue();

        var notExists = await repository.ExistsByTitleAsync("NonExisting");
        notExists.Should().BeFalse();
    }

    [Fact]
    public async Task GetPagedListAsync_WhenEmpty_ShouldReturnZeroCountAndEmptyList()
    {
        await using var context = CreateInMemoryDbContext();
        var repository = new SeriesRepository(context);

        var (items, totalCount) = await repository.GetPagedListAsync(1, 10);

        totalCount.Should().Be(0);
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPagedListAsync_WhenItemsExist_ShouldPaginateProperly()
    {
        await using var context = CreateInMemoryDbContext();
        var repository = new SeriesRepository(context);

        for (var i = 1; i <= 15; i++)
        {
            repository.Add(Series.Create($"Series {i:D2}", $"Description {i}", false));
        }
        await context.SaveChangesAsync();

        var (items, totalCount) = await repository.GetPagedListAsync(1, 10);

        totalCount.Should().Be(15);
        items.Should().HaveCount(10);

        var (secondPageItems, _) = await repository.GetPagedListAsync(2, 10);
        secondPageItems.Should().HaveCount(5);
    }
}
