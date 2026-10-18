using Moq;
using ShuffleSeries.Catalog.Application.Features.Series.Queries.GetSeriesById;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;

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
    public async Task Handle_Should_ThrowSeriesNotFoundException_When_SeriesDoesNotExist()
    {
        // Arrange
        var query = new GetSeriesByIdQuery(Guid.NewGuid());

        _seriesRepositoryMock.Setup(x => x.GetByIdWithTranslationsAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Series?)null);

        // Act
        Func<Task> action = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<SeriesNotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ReturnSeriesResponse_When_SeriesExists()
    {
        // Arrange
        var series = Series.Create(
            releaseYear: 2020,
            seriesType: SeriesType.EpisodicAnthology,
            posterUrl: "https://example.com/poster.jpg",
            totalSeasons: 2);

        series.AddOrUpdateTranslation("tr", "Test Dizi", "Test Açıklama", "Harika");

        var query = new GetSeriesByIdQuery(series.Id);

        _seriesRepositoryMock.Setup(x => x.GetByIdWithTranslationsAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(series.Id);
        result.ReleaseYear.Should().Be(2020);
        result.SeriesType.Should().Be(SeriesType.EpisodicAnthology);
        result.Translations.Should().HaveCount(1);
        result.Translations[0].Title.Should().Be("Test Dizi");
        result.Translations[0].Overview.Should().Be("Test Açıklama");
    }
}
