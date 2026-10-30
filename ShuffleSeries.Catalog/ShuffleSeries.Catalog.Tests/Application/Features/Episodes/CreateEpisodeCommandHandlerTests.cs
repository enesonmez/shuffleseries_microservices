using Moq;
using ShuffleSeries.Catalog.Application.Features.Episodes.Commands.CreateEpisode;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Episodes;

public class CreateEpisodeCommandHandlerTests
{
    private readonly Mock<ISeriesRepository> _seriesRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CreateEpisodeCommandHandler _handler;

    public CreateEpisodeCommandHandlerTests()
    {
        _seriesRepositoryMock = new Mock<ISeriesRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new CreateEpisodeCommandHandler(_seriesRepositoryMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_AddEpisodeToSeason_When_SeriesExists()
    {
        // Arrange
        var series = Series.Create(
            42009,
            "tt2085059",
            2020,
            SeriesType.Standard,
            "https://example.com/poster.jpg");

        series.AddOrUpdateTranslation(LanguageCode.Turkish, "Series Title", "Series Overview");

        var command = new CreateEpisodeCommand(
            series.Id,
            1,
            1,
            "Episode 1",
            "Overview 1",
            "tr",
            IsStandalone: false,
            RuntimeMinutes: 45);

        _seriesRepositoryMock.Setup(x => x.GetByIdWithSeasonsAsync(series.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(series.Seasons.Single().Episodes.Single().Id);
        series.Seasons.Should().ContainSingle(s => s.SeasonNumber == 1);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowSeriesNotFoundException_When_SeriesDoesNotExist()
    {
        // Arrange
        var command = new CreateEpisodeCommand(
            Guid.NewGuid(),
            1,
            1,
            "Episode 1",
            "Overview 1",
            "tr");

        _seriesRepositoryMock.Setup(x => x.GetByIdWithSeasonsAsync(command.SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Series?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<SeriesNotFoundException>();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
