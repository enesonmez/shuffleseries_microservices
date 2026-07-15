using FluentAssertions;
using Moq;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.UpdateSeries;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Catalog.Domain.Services;
using ShuffleSeries.Shared.Core.Domain.Repositories;
using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Tests.Application.Features.UpdateSeries;

public class UpdateSeriesCommandHandlerTests
{
    private readonly Mock<ISeriesRepository> _seriesRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly SeriesDomainService _seriesDomainService;
    private readonly UpdateSeriesCommandHandler _handler;

    public UpdateSeriesCommandHandlerTests()
    {
        _seriesRepositoryMock = new Mock<ISeriesRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _seriesDomainService = new SeriesDomainService(_seriesRepositoryMock.Object);

        _handler = new UpdateSeriesCommandHandler(
            _seriesRepositoryMock.Object,
            _seriesDomainService,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_SeriesDoesNotExist()
    {
        // Arrange
        var command = new UpdateSeriesCommand(Guid.NewGuid(), "Title", "Desc", false);

        _seriesRepositoryMock.Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Series?)null);

        // Act
        Func<Task> action = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_UpdateAndSaveChanges_When_TitleIsUnchanged()
    {
        // Arrange
        var existingSeries = Series.Create("Same Title", "Old Desc", false);
        var command = new UpdateSeriesCommand(existingSeries.Id, "Same Title", "New Desc", true);

        _seriesRepositoryMock.Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSeries);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        existingSeries.Description.Should().Be("New Desc");
        existingSeries.IsIndependentEpisodes.Should().BeTrue();

        _seriesRepositoryMock.Verify(x => x.ExistsByTitleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _seriesRepositoryMock.Verify(x => x.Update(existingSeries), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_CallDomainService_When_TitleIsChanged()
    {
        // Arrange
        var existingSeries = Series.Create("Old Title", "Old Desc", false);
        var command = new UpdateSeriesCommand(existingSeries.Id, "New Title", "New Desc", true);

        _seriesRepositoryMock.Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSeries);

        _seriesRepositoryMock.Setup(x => x.ExistsByTitleAsync("New Title", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        existingSeries.Title.Should().Be("New Title");

        _seriesRepositoryMock.Verify(x => x.ExistsByTitleAsync("New Title", It.IsAny<CancellationToken>()), Times.Once);

        _seriesRepositoryMock.Verify(x => x.Update(existingSeries), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}