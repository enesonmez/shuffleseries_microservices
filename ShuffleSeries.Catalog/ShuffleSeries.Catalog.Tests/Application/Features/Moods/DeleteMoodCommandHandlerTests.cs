using Moq;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Application.Features.Moods.Commands.DeleteMood;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Moods;

public class DeleteMoodCommandHandlerTests
{
    private readonly Mock<IMoodRepository> _moodRepositoryMock = new();
    private readonly Mock<ICatalogCacheService> _cacheServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly DeleteMoodCommandHandler _handler;

    public DeleteMoodCommandHandlerTests()
    {
        _handler = new DeleteMoodCommandHandler(
            _moodRepositoryMock.Object,
            _cacheServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_MoodDoesNotExist()
    {
        var command = new DeleteMoodCommand(Guid.NewGuid());

        _moodRepositoryMock.Setup(x => x.GetByIdWithTranslationsAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mood?)null);

        var action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<MoodNotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_DeleteMoodAndInvalidateCache_When_MoodExists()
    {
        var mood = Mood.Create("chill", "#4A90E2");
        var command = new DeleteMoodCommand(mood.Id);

        _moodRepositoryMock.Setup(x => x.GetByIdWithTranslationsAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mood);

        await _handler.Handle(command, CancellationToken.None);

        mood.IsDeleted.Should().BeTrue();
        _moodRepositoryMock.Verify(x => x.Delete(mood), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.Remove(CatalogCacheKeys.Moods), Times.Once);
    }
}
