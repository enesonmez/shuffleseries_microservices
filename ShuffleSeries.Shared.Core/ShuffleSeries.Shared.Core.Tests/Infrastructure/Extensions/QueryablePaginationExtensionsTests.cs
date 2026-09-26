using AwesomeAssertions;
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
}
