using Moq;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Application.Features.Moods.Commands.UpdateMood;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Moods;

public class UpdateMoodCommandHandlerTests
{
    private readonly Mock<IMoodRepository> _moodRepositoryMock = new();
    private readonly Mock<ICatalogCacheService> _cacheServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly UpdateMoodCommandHandler _handler;

    public UpdateMoodCommandHandlerTests()
    {
        _handler = new UpdateMoodCommandHandler(
            _moodRepositoryMock.Object,
            _cacheServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_MoodDoesNotExist()
    {
        var command = new UpdateMoodCommand(Guid.NewGuid(), "chill", "#FFFFFF", null, 1);

        _moodRepositoryMock.Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mood?)null);

        var action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<MoodNotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ThrowMoodCodeAlreadyExistsException_When_CodeChangedAndExists()
    {
        var mood = Mood.Create("chill", "#000000", null, 1);
        var command = new UpdateMoodCommand(mood.Id, "cozy", "#FFFFFF", null, 2);

        _moodRepositoryMock.Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mood);

        _moodRepositoryMock.Setup(x => x.ExistsByCodeAsync("cozy", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<MoodCodeAlreadyExistsException>();
    }

    [Fact]
    public async Task Handle_Should_UpdateMood_And_InvalidateCache_When_Valid()
    {
        var mood = Mood.Create("chill", "#000000", null, 1);
        var command = new UpdateMoodCommand(mood.Id, "chill-updated", "#FFFFFF", "https://example.com/icon.png", 5);

        _moodRepositoryMock.Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mood);

        _moodRepositoryMock.Setup(x => x.ExistsByCodeAsync("chill-updated", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _handler.Handle(command, CancellationToken.None);

        mood.Code.Should().Be("chill-updated");
        mood.AmbientColorHex.Should().Be("#FFFFFF");
        mood.DisplayOrder.Should().Be(5);

        _moodRepositoryMock.Verify(x => x.Update(mood), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.Remove(CatalogCacheKeys.Moods), Times.Once);
    }
}
