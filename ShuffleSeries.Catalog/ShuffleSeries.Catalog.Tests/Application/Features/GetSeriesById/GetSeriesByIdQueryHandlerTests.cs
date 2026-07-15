using FluentAssertions;
using Moq;
using ShuffleSeries.Catalog.Application.Features.Series.Queries.GetSeriesById;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Tests.Application.Features.GetSeriesById;

public class GetSeriesByIdQueryHandlerTests
{
    private readonly Mock<ISeriesRepository> _seriesRepositoryMock;
    private readonly GetSeriesByIdQueryHandler _handler;

    public GetSeriesByIdQueryHandlerTests()
    {
        _seriesRepositoryMock = new Mock<ISeriesRepository>();
        _handler = new GetSeriesByIdQueryHandler(_seriesRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_SeriesDoesNotExist()
    {
        // Arrange
        var query = new GetSeriesByIdQuery(Guid.NewGuid());

        _seriesRepositoryMock.Setup(x => x.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Series?)null);

        // Act
        Func<Task> action = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ReturnSeriesResponse_When_SeriesExists()
    {
        // Arrange
        var series = Series.Create("Test Series", "Test Description", false);
        var query = new GetSeriesByIdQuery(series.Id);

        _seriesRepositoryMock.Setup(x => x.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(series.Id);
        result.Title.Should().Be("Test Series");
        result.Description.Should().Be("Test Description");
    }
}