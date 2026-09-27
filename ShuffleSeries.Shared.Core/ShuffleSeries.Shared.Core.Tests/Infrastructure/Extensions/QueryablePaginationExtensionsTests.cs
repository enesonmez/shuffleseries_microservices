using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Shared.Core.Application.Requests;
using ShuffleSeries.Shared.Core.Infrastructure.Extensions;

namespace ShuffleSeries.Shared.Core.Tests.Infrastructure.Extensions;

public class QueryablePaginationExtensionsTests
{
    [Fact]
    public void ApplyPagination_WithValidPaginationRequest_AppliesCorrectSkipAndTake()
    {
        // Arrange
        var source = Enumerable.Range(1, 50).AsQueryable();
        var pagination = new PaginationRequest(pageNumber: 2, pageSize: 10);

        // Act
        var result = source.ApplyPagination(pagination).ToList();

        // Assert
        result.Should().HaveCount(10);
        result.Should().Equal(Enumerable.Range(11, 10));
    }

    [Fact]
    public void ApplyPagination_WithNullRequest_AppliesDefaultPageAndSize()
    {
        // Arrange
        var source = Enumerable.Range(1, 50).AsQueryable();

        // Act
        var result = source.ApplyPagination((PaginationRequest?)null).ToList();

        // Assert
        result.Should().HaveCount(10);
        result.Should().Equal(Enumerable.Range(1, 10));
    }

    [Theory]
    [InlineData(0, 0, 10)]
    [InlineData(-1, -10, 10)]
    [InlineData(1, 15, 15)]
    [InlineData(1, 200, 50)] // Total items is 50, Take is 100 clamped
    public void ApplyPagination_WithPageAndPageSizeIntegers_NormalizesSafely(
        int? page, int? pageSize, int expectedCount)
    {
        // Arrange
        var source = Enumerable.Range(1, 50).AsQueryable();

        // Act
        var result = source.ApplyPagination(page, pageSize).ToList();

        // Assert
        result.Should().HaveCount(expectedCount);
    }

    private sealed class DummyEntity
    {
        public int Id { get; set; }
    }

    private sealed class DummyPaginationDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public Microsoft.EntityFrameworkCore.DbSet<DummyEntity> Entities => Set<DummyEntity>();
        public DummyPaginationDbContext(Microsoft.EntityFrameworkCore.DbContextOptions<DummyPaginationDbContext> options) : base(options) { }
    }

    [Fact]
    public async Task ToPaginatedListAsync_ShouldReturnPaginatedList()
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<DummyPaginationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new DummyPaginationDbContext(options);
        db.Entities.AddRange(Enumerable.Range(1, 25).Select(i => new DummyEntity { Id = i }));
        await db.SaveChangesAsync();

        var paginated1 = await db.Entities.ToPaginatedListAsync(new PaginationRequest(2, 10));
        paginated1.TotalCount.Should().Be(25);
        paginated1.Items.Should().HaveCount(10);
        paginated1.PageNumber.Should().Be(2);

        var paginated2 = await db.Entities.ToPaginatedListAsync(1, 5);
        paginated2.TotalCount.Should().Be(25);
        paginated2.Items.Should().HaveCount(5);

        var paginatedNull = await db.Entities.ToPaginatedListAsync((PaginationRequest?)null);
        paginatedNull.TotalCount.Should().Be(25);
        paginatedNull.Items.Should().HaveCount(10);
    }
}
