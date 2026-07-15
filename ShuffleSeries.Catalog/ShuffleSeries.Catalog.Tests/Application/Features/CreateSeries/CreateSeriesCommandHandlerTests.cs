using FluentAssertions;
using Moq;
using ShuffleSeries.Catalog.Application.Features.Series.Commands.CreateSeries;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Catalog.Domain.Services;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.CreateSeries;

public class CreateSeriesCommandHandlerTests
{
    private readonly Mock<ISeriesRepository> _seriesRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly SeriesDomainService _seriesDomainService;
    private readonly CreateSeriesCommandHandler _handler;

    public CreateSeriesCommandHandlerTests()
    {
        _seriesRepositoryMock = new Mock<ISeriesRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _seriesDomainService = new SeriesDomainService(_seriesRepositoryMock.Object);
        _handler = new CreateSeriesCommandHandler(_unitOfWorkMock.Object, _seriesRepositoryMock.Object,
            _seriesDomainService);
    }

    [Fact]
    public async Task Handle_Should_CreateSeriesAndReturnId_When_TitleIsUnique()
    {
        // Arrange
        var command = new CreateSeriesCommand("Dexter", "Serial killer analyst", false);

        _seriesRepositoryMock.Setup(x => x.ExistsByTitleAsync(command.Title, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();

        _seriesRepositoryMock.Verify(x => x.Add(It.Is<Series>(s => s.Title == command.Title)), Times.Once);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task Handle_Should_ThrowSeriesTitleAlreadyExistsException_When_TitleIsNotUnique()
    {
        // Arrange
        var command = new CreateSeriesCommand("Dexter", "Serial killer analyst", false);
        
        _seriesRepositoryMock.Setup(x => x.ExistsByTitleAsync(command.Title, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> action = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<SeriesTitleAlreadyExistsException>();
        
        _seriesRepositoryMock.Verify(x => x.Add(It.IsAny<Series>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}