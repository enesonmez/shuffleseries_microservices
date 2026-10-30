using Moq;
using ShuffleSeries.Catalog.Application.Common.Constants;
using ShuffleSeries.Catalog.Application.Common.Interfaces;
using ShuffleSeries.Catalog.Application.Features.Moods.Commands.Translations.AddOrUpdateMoodTranslation;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Moods;

public class AddOrUpdateMoodTranslationCommandHandlerTests
{
    private readonly Mock<IMoodRepository> _moodRepositoryMock;
    private readonly Mock<ICatalogCacheService> _cacheServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly AddOrUpdateMoodTranslationCommandHandler _handler;

    public AddOrUpdateMoodTranslationCommandHandlerTests()
    {
        _moodRepositoryMock = new Mock<IMoodRepository>();
        _cacheServiceMock = new Mock<ICatalogCacheService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new AddOrUpdateMoodTranslationCommandHandler(
            _moodRepositoryMock.Object,
            _cacheServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowMoodNotFoundException_When_MoodDoesNotExist()
    {
        // Arrange
        var command = new AddOrUpdateMoodTranslationCommand(Guid.NewGuid(), "tr", "İsim", "Açıklama");
        _moodRepositoryMock.Setup(x => x.GetByIdWithTranslationsAsync(command.MoodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mood?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MoodNotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_AddOrUpdateTranslation_And_InvalidateCache_When_Valid()
    {
        // Arrange
        var mood = Mood.Create("code", "#FF0000");
        var command = new AddOrUpdateMoodTranslationCommand(mood.Id, "tr", "Yeni Başlık", "Yeni Açıklama", "tag1");

        _moodRepositoryMock.Setup(x => x.GetByIdWithTranslationsAsync(command.MoodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mood);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        mood.Translations.Should().ContainSingle(t => t.LanguageCode == "tr" && t.Name == "Yeni Başlık");
        _moodRepositoryMock.Verify(x => x.Update(mood), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.RemoveAsync(CatalogCacheKeys.Moods, It.IsAny<CancellationToken>()), Times.Once);
    }
}
