using Moq;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Application.Features.Moods.Commands.CreateMood;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Moods;

public class CreateMoodCommandHandlerTests
{
    private readonly Mock<IMoodRepository> _moodRepositoryMock = new();
    private readonly Mock<ICatalogCacheService> _cacheServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly CreateMoodCommandHandler _handler;

    public CreateMoodCommandHandlerTests()
    {
        _handler = new CreateMoodCommandHandler(
            _moodRepositoryMock.Object,
            _cacheServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowMoodCodeAlreadyExistsException_When_CodeExists()
    {
        var command = new CreateMoodCommand(
            Code: "chill",
            Name: "Chill",
            Description: "Relaxing vibes",
            LanguageCode: "en",
            AmbientColorHex: "#4A90E2",
            IconUrl: null,
            DisplayOrder: 1,
            IsActive: true,
            VibeTags: null);

        _moodRepositoryMock.Setup(x => x.ExistsByCodeAsync(command.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<MoodCodeAlreadyExistsException>();
    }

    [Fact]
    public async Task Handle_Should_CreateMood_And_InvalidateCache_When_Valid()
    {
        var command = new CreateMoodCommand(
            Code: "chill",
            Name: "Chill",
            Description: "Relaxing vibes",
            LanguageCode: "en",
            AmbientColorHex: "#4A90E2",
            IconUrl: null,
            DisplayOrder: 1,
            IsActive: true,
            VibeTags: "relax,calm");

        _moodRepositoryMock.Setup(x => x.ExistsByCodeAsync(command.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        _moodRepositoryMock.Verify(x => x.Add(It.Is<Mood>(m => m.Code == "chill")), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.Remove(CatalogCacheKeys.Moods), Times.Once);
    }
}
