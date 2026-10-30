using Moq;
using ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Translations.AddOrUpdateEpisodeTranslation;
using ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Translations.RemoveEpisodeTranslation;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Episodes;

public class EpisodeTranslationCommandHandlerTests
{
    private readonly Mock<IEpisodeRepository> _episodeRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private Episode CreateSampleEpisode()
    {
        var series = Series.Create(42009, "tt2085059", 2011, SeriesType.EpisodicAnthology, "https://example.com/poster.jpg");
        return series.AddEpisode(1, 1, isStandalone: true);
    }

    [Fact]
    public async Task AddOrUpdateEpisodeTranslation_WhenValid_ShouldAddTranslationAndSaveChanges()
    {
        // Arrange
        var episode = CreateSampleEpisode();

        _episodeRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(episode.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(episode);

        var handler = new AddOrUpdateEpisodeTranslationCommandHandler(
            _episodeRepositoryMock.Object,
            _unitOfWorkMock.Object);

        var command = new AddOrUpdateEpisodeTranslationCommand(episode.Id, "tr", "Milli Marş", "Black Mirror 1. Bölüm");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        episode.Translations.Should().HaveCount(1);
        episode.Translations.First().LanguageCode.Value.Should().Be("tr");
        episode.Translations.First().Title.Should().Be("Milli Marş");

        _episodeRepositoryMock.Verify(x => x.Update(episode), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddOrUpdateEpisodeTranslation_WhenEpisodeNotFound_ShouldThrowEpisodeNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _episodeRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Episode?)null);

        var handler = new AddOrUpdateEpisodeTranslationCommandHandler(
            _episodeRepositoryMock.Object,
            _unitOfWorkMock.Object);

        var command = new AddOrUpdateEpisodeTranslationCommand(nonExistentId, "tr", "Başlık");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EpisodeNotFoundException>();
    }

    [Fact]
    public async Task RemoveEpisodeTranslation_WhenEpisodeExists_ShouldRemoveTranslationAndSaveChanges()
    {
        // Arrange
        var episode = CreateSampleEpisode();
        episode.AddOrUpdateTranslation("en", "National Anthem", "Overview");

        _episodeRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(episode.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(episode);

        var handler = new RemoveEpisodeTranslationCommandHandler(
            _episodeRepositoryMock.Object,
            _unitOfWorkMock.Object);

        var command = new RemoveEpisodeTranslationCommand(episode.Id, "en");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        episode.Translations.Should().BeEmpty();
        _episodeRepositoryMock.Verify(x => x.Update(episode), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveEpisodeTranslation_WhenEpisodeNotFound_ShouldThrowEpisodeNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _episodeRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Episode?)null);

        var handler = new RemoveEpisodeTranslationCommandHandler(
            _episodeRepositoryMock.Object,
            _unitOfWorkMock.Object);

        var command = new RemoveEpisodeTranslationCommand(nonExistentId, "en");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EpisodeNotFoundException>();
    }

    [Fact]
    public void AddOrUpdateEpisodeTranslationValidator_WhenInvalid_ShouldHaveValidationErrors()
    {
        // Arrange
        var validator = new AddOrUpdateEpisodeTranslationCommandValidator();
        var invalidCommand = new AddOrUpdateEpisodeTranslationCommand(Guid.Empty, "invalid-lang", "");

        // Act
        var result = validator.Validate(invalidCommand);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddOrUpdateEpisodeTranslationCommand.EpisodeId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddOrUpdateEpisodeTranslationCommand.Title));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddOrUpdateEpisodeTranslationCommand.LanguageCode));
    }

    [Fact]
    public void RemoveEpisodeTranslationValidator_WhenInvalid_ShouldHaveValidationErrors()
    {
        // Arrange
        var validator = new RemoveEpisodeTranslationCommandValidator();
        var invalidCommand = new RemoveEpisodeTranslationCommand(Guid.Empty, "invalid-lang");

        // Act
        var result = validator.Validate(invalidCommand);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RemoveEpisodeTranslationCommand.EpisodeId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RemoveEpisodeTranslationCommand.LanguageCode));
    }

    [Fact]
    public void RemoveEpisodeTranslationValidator_WhenValid_ShouldPassValidation()
    {
        // Arrange
        var validator = new RemoveEpisodeTranslationCommandValidator();
        var validCommand = new RemoveEpisodeTranslationCommand(Guid.NewGuid(), "tr");

        // Act
        var result = validator.Validate(validCommand);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
