using AwesomeAssertions;
using ShuffleSeries.Shared.Core.Application.Requests;

namespace ShuffleSeries.Shared.Core.Tests.Application.Requests;

public class PaginationRequestTests
{
    [Fact]
    public void DefaultConstructor_ShouldInitialize_WithDefaultBounds()
    {
        // Act
        var request = new PaginationRequest();

        // Assert
        request.PageNumber.Should().Be(1);
        request.PageSize.Should().Be(10);
        request.Page.Should().Be(1);
        request.Skip.Should().Be(0);
        request.Take.Should().Be(10);
    }

    [Theory]
    [InlineData(1, 10, 0, 10)]
    [InlineData(2, 10, 10, 10)]
    [InlineData(3, 20, 40, 20)]
    [InlineData(5, 5, 20, 5)]
    public void SkipAndTake_ShouldBeCalculatedCorrectly_ForValidInputs(
        int page, int pageSize, int expectedSkip, int expectedTake)
    {
        // Act
        var request = new PaginationRequest(page, pageSize);

        // Assert
        request.PageNumber.Should().Be(page);
        request.PageSize.Should().Be(pageSize);
        request.Skip.Should().Be(expectedSkip);
        request.Take.Should().Be(expectedTake);
    }

    [Theory]
    [InlineData(null, null, 1, 10, 0)]
    [InlineData(0, 0, 1, 10, 0)]
    [InlineData(-1, -50, 1, 10, 0)]
    [InlineData(-100, 25, 1, 25, 0)]
    [InlineData(1, 500, 1, 100, 0)] // MaxPageSize boundary check
    [InlineData(2, 1000, 2, 100, 100)] // Page 2 with clamped max page size
    public void Constructor_ShouldNormalizeAndClamp_InvalidOrBoundaryInputs(
        int? inputPage, int? inputPageSize, int expectedPage, int expectedPageSize, int expectedSkip)
    {
        // Act
        var request = new PaginationRequest(inputPage, inputPageSize);

        // Assert
        request.PageNumber.Should().Be(expectedPage);
        request.PageSize.Should().Be(expectedPageSize);
        request.Skip.Should().Be(expectedSkip);
        request.Take.Should().Be(expectedPageSize);
    }

    [Fact]
    public void CreateFactoryMethod_ShouldReturnSameNormalizedInstance()
    {
        // Act
        var request = PaginationRequest.Create(0, 500);

        // Assert
        request.PageNumber.Should().Be(1);
        request.PageSize.Should().Be(100);
        request.Skip.Should().Be(0);
        request.Take.Should().Be(100);
    }
}
