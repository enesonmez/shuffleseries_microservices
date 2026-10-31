using Moq;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Application.Features.Moods.Commands.Translations.RemoveMoodTranslation;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Moods;

public class RemoveMoodTranslationCommandHandlerTests
{
    private readonly Mock<IMoodRepository> _moodRepositoryMock;
    private readonly Mock<ICatalogCacheService> _cacheServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly RemoveMoodTranslationCommandHandler _handler;

    public RemoveMoodTranslationCommandHandlerTests()
    {
        _moodRepositoryMock = new Mock<IMoodRepository>();
        _cacheServiceMock = new Mock<ICatalogCacheService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new RemoveMoodTranslationCommandHandler(
            _moodRepositoryMock.Object,
            _cacheServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowMoodNotFoundException_When_MoodDoesNotExist()
    {
        // Arrange
        var command = new RemoveMoodTranslationCommand(Guid.NewGuid(), "tr");
        _moodRepositoryMock.Setup(x => x.GetByIdWithTranslationsAsync(command.MoodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mood?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MoodNotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_RemoveTranslation_And_InvalidateCache_When_Valid()
    {
        // Arrange
        var mood = Mood.Create("chill", "#123456", "https://example.com/icon.png");
        mood.AddOrUpdateTranslation(LanguageCode.Turkish, "Sakin", "Açıklama", "rahat");
        mood.AddOrUpdateTranslation(LanguageCode.English, "Chill", "Description", "relaxed");

        var command = new RemoveMoodTranslationCommand(mood.Id, "en");

        _moodRepositoryMock.Setup(x => x.GetByIdWithTranslationsAsync(mood.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mood);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        mood.Translations.Should().NotContain(t => t.LanguageCode == LanguageCode.English);
        mood.Translations.Should().ContainSingle(t => t.LanguageCode == LanguageCode.Turkish);

        _moodRepositoryMock.Verify(x => x.Update(mood), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.RemoveAsync(CatalogCacheKeys.Moods, It.IsAny<CancellationToken>()), Times.Once);
    }
}
