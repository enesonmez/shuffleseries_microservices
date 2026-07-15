using FluentAssertions;
using Moq;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.DeleteSeries;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;
using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Catalog.Tests.Application.Features.DeleteSeries;

public class DeleteSeriesCommandHandlerTests
{
    private readonly Mock<ISeriesRepository> _seriesRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly DeleteSeriesCommandHandler _handler;

    public DeleteSeriesCommandHandlerTests()
    {
        _seriesRepositoryMock = new Mock<ISeriesRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new DeleteSeriesCommandHandler(_seriesRepositoryMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_SeriesDoesNotExist()
    {
        // Arrange
        var command = new DeleteSeriesCommand(Guid.NewGuid());

        _seriesRepositoryMock.Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Series?)null);

        // Act
        Func<Task> action = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Series with ID {command.Id} was not found.");

        _seriesRepositoryMock.Verify(x => x.Delete(It.IsAny<Series>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_CallDeleteAndSaveChanges_When_SeriesExists()
    {
        // Arrange
        var existingSeries = Series.Create("Title to Delete", "Description", false);
        var command = new DeleteSeriesCommand(existingSeries.Id);

        _seriesRepositoryMock.Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSeries);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _seriesRepositoryMock.Verify(x => x.Delete(existingSeries), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}