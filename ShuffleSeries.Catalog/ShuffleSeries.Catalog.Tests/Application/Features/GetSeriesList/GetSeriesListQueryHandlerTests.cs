using FluentAssertions;
using Moq;
using ShuffleSeries.Catalog.Application.Features.Series.Queries.GetSeriesList;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.GetSeriesList;

public class GetSeriesListQueryHandlerTests
{
    private readonly Mock<ISeriesRepository> _seriesRepositoryMock;
    private readonly GetSeriesListQueryHandler _handler;

    public GetSeriesListQueryHandlerTests()
    {
        _seriesRepositoryMock = new Mock<ISeriesRepository>();
        _handler = new GetSeriesListQueryHandler(_seriesRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnPaginatedList_When_Called()
    {
        // Arrange
        var query = new GetSeriesListQuery(1, 10);
        var seriesList = new List<Series>
        {
            Series.Create("Series 1", "Desc 1", false),
            Series.Create("Series 2", "Desc 2", true)
        };

        _seriesRepositoryMock.Setup(x => x.GetPagedListAsync(query.Page, query.PageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync((seriesList, seriesList.Count));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.First().Title.Should().Be("Series 1");
    }

    [Theory]
    [InlineData(0, 0, 1, 10)]
    [InlineData(-5, -20, 1, 10)]
    [InlineData(2, 500, 2, 100)]
    [InlineData(5, 50, 5, 50)]
    public void Query_Should_Normalize_Page_And_PageSize_Bounds(
        int inputPage, int inputPageSize, int expectedPage, int expectedPageSize)
    {
        // Act
        var query = new GetSeriesListQuery(inputPage, inputPageSize);

        // Assert
        query.Page.Should().Be(expectedPage);
        query.PageSize.Should().Be(expectedPageSize);
    }

    [Fact]
    public void Query_Default_Constructor_Should_Set_Default_Bounds()
    {
        // Act
        var query = new GetSeriesListQuery();

        // Assert
        query.Page.Should().Be(1);
        query.PageSize.Should().Be(10);
    }
}