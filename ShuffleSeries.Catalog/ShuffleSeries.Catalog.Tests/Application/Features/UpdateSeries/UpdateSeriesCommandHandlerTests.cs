using Moq;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.UpdateSeries;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.UpdateSeries;

public class UpdateSeriesCommandHandlerTests
{
    private readonly Mock<ISeriesRepository> _seriesRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UpdateSeriesCommandHandler _handler;

    public UpdateSeriesCommandHandlerTests()
    {
        _seriesRepositoryMock = new Mock<ISeriesRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new UpdateSeriesCommandHandler(
            _seriesRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowSeriesNotFoundException_When_SeriesDoesNotExist()
    {
        // Arrange
        var command = new UpdateSeriesCommand(Guid.NewGuid(), 2020, SeriesType.Standard, 3);

        _seriesRepositoryMock.Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Series?)null);

        // Act
        Func<Task> action = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<SeriesNotFoundException>();
        _seriesRepositoryMock.Verify(x => x.Update(It.IsAny<Series>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_UpdateDetailsAndSaveChanges_When_SeriesExists()
    {
        // Arrange
        var existingSeries = Series.Create(2015, SeriesType.Standard, "https://example.com/poster.jpg");
        var command = new UpdateSeriesCommand(
            existingSeries.Id,
            2016,
            SeriesType.EpisodicAnthology,
            5,
            "tt1234567",
            8.9,
            120000);

        _seriesRepositoryMock.Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSeries);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        existingSeries.ReleaseYear.Should().Be(2016);
        existingSeries.SeriesType.Should().Be(SeriesType.EpisodicAnthology);
        existingSeries.TotalSeasons.Should().Be(5);
        existingSeries.ImdbId.Should().Be("tt1234567");
        existingSeries.ImdbRating.Should().Be(8.9);
        existingSeries.ImdbVotes.Should().Be(120000);

        _seriesRepositoryMock.Verify(x => x.Update(existingSeries), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowDuplicateImdbIdException_When_ImdbIdBelongsToAnotherMedia()
    {
        // Arrange
        var existingSeries = Series.Create(2015, SeriesType.Standard, "https://example.com/poster.jpg");
        var command = new UpdateSeriesCommand(
            existingSeries.Id,
            2016,
            SeriesType.EpisodicAnthology,
            5,
            "tt1375666",
            8.9,
            120000);

        _seriesRepositoryMock.Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSeries);
        _seriesRepositoryMock.Setup(x => x.ExistsByImdbIdAsync("tt1375666", existingSeries.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> action = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<DuplicateImdbIdException>();
        _seriesRepositoryMock.Verify(x => x.Update(It.IsAny<Series>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
