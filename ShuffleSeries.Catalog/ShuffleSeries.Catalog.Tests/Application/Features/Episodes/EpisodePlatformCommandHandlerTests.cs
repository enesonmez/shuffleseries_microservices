using Moq;
using ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Platforms.AddEpisodePlatform;
using ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Platforms.RemoveEpisodePlatform;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Episodes;

public class EpisodePlatformCommandHandlerTests
{
    private readonly Mock<IEpisodeRepository> _episodeRepositoryMock = new();
    private readonly Mock<IPlatformRepository> _platformRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private Episode CreateSampleEpisode()
    {
        var series = Series.Create(42009, "tt2085059", 2011, SeriesType.EpisodicAnthology, "https://example.com/poster.jpg");
        return series.AddEpisode(1, 1, isStandalone: true);
    }

    private Platform CreateSamplePlatform()
    {
        return Platform.Create(
            "Netflix",
            "netflix",
            "https://netflix.com/logo.png",
            "https://netflix.com/off.png",
            "https://netflix.com");
    }

    [Fact]
    public async Task AddEpisodePlatform_WhenValid_ShouldAddPlatformAndSaveChanges()
    {
        // Arrange
        var episode = CreateSampleEpisode();
        var platform = CreateSamplePlatform();

        _episodeRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(episode.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(episode);
        _platformRepositoryMock.Setup(x => x.GetByIdAsync(platform.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(platform);

        var handler = new AddEpisodePlatformCommandHandler(
            _episodeRepositoryMock.Object,
            _platformRepositoryMock.Object,
            _unitOfWorkMock.Object);

        var command = new AddEpisodePlatformCommand(episode.Id, platform.Id, "https://netflix.com/watch/123", "nflx://watch/123");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        episode.EpisodePlatforms.Should().HaveCount(1);
        episode.EpisodePlatforms.First().PlatformId.Should().Be(platform.Id);
        episode.EpisodePlatforms.First().WebUrl.Should().Be("https://netflix.com/watch/123");
        episode.EpisodePlatforms.First().AppSchemeUrl.Should().Be("nflx://watch/123");

        _episodeRepositoryMock.Verify(x => x.Update(episode), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddEpisodePlatform_WhenPlatformAlreadyExists_ShouldThrowDuplicateEpisodePlatformException()
    {
        // Arrange
        var episode = CreateSampleEpisode();
        var platform = CreateSamplePlatform();
        episode.AddPlatform(platform.Id, "https://netflix.com/watch/old", "nflx://watch/old");

        _episodeRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(episode.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(episode);
        _platformRepositoryMock.Setup(x => x.GetByIdAsync(platform.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(platform);

        var handler = new AddEpisodePlatformCommandHandler(
            _episodeRepositoryMock.Object,
            _platformRepositoryMock.Object,
            _unitOfWorkMock.Object);

        var command = new AddEpisodePlatformCommand(episode.Id, platform.Id, "https://netflix.com/watch/updated", "nflx://watch/updated");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DuplicateEpisodePlatformException>();
        _episodeRepositoryMock.Verify(x => x.Update(It.IsAny<Episode>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddEpisodePlatform_WhenEpisodeNotFound_ShouldThrowEpisodeNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _episodeRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Episode?)null);

        var handler = new AddEpisodePlatformCommandHandler(
            _episodeRepositoryMock.Object,
            _platformRepositoryMock.Object,
            _unitOfWorkMock.Object);

        var command = new AddEpisodePlatformCommand(nonExistentId, Guid.NewGuid(), "https://netflix.com/watch/123");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EpisodeNotFoundException>();
    }

    [Fact]
    public async Task AddEpisodePlatform_WhenPlatformNotFound_ShouldThrowPlatformNotFoundException()
    {
        // Arrange
        var episode = CreateSampleEpisode();
        var nonExistentPlatformId = Guid.NewGuid();

        _episodeRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(episode.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(episode);
        _platformRepositoryMock.Setup(x => x.GetByIdAsync(nonExistentPlatformId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Platform?)null);

        var handler = new AddEpisodePlatformCommandHandler(
            _episodeRepositoryMock.Object,
            _platformRepositoryMock.Object,
            _unitOfWorkMock.Object);

        var command = new AddEpisodePlatformCommand(episode.Id, nonExistentPlatformId, "https://netflix.com/watch/123");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PlatformNotFoundException>();
    }

    [Fact]
    public async Task RemoveEpisodePlatform_WhenEpisodeExists_ShouldRemovePlatformAndSaveChanges()
    {
        // Arrange
        var episode = CreateSampleEpisode();
        var platformId = Guid.NewGuid();
        episode.AddPlatform(platformId, "https://netflix.com/watch/123");

        _episodeRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(episode.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(episode);

        var handler = new RemoveEpisodePlatformCommandHandler(
            _episodeRepositoryMock.Object,
            _unitOfWorkMock.Object);

        var command = new RemoveEpisodePlatformCommand(episode.Id, platformId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        episode.EpisodePlatforms.Should().BeEmpty();
        _episodeRepositoryMock.Verify(x => x.Update(episode), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveEpisodePlatform_WhenEpisodeNotFound_ShouldThrowEpisodeNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _episodeRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Episode?)null);

        var handler = new RemoveEpisodePlatformCommandHandler(
            _episodeRepositoryMock.Object,
            _unitOfWorkMock.Object);

        var command = new RemoveEpisodePlatformCommand(nonExistentId, Guid.NewGuid());

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EpisodeNotFoundException>();
    }

    [Fact]
    public void AddEpisodePlatformValidator_WhenInvalid_ShouldHaveValidationErrors()
    {
        // Arrange
        var validator = new AddEpisodePlatformCommandValidator();
        var invalidCommand = new AddEpisodePlatformCommand(Guid.Empty, Guid.Empty, "not-a-valid-url", "ftp://invalid-scheme");

        // Act
        var result = validator.Validate(invalidCommand);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddEpisodePlatformCommand.EpisodeId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddEpisodePlatformCommand.PlatformId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddEpisodePlatformCommand.WebUrl));
    }

    [Fact]
    public void RemoveEpisodePlatformValidator_WhenInvalid_ShouldHaveValidationErrors()
    {
        // Arrange
        var validator = new RemoveEpisodePlatformCommandValidator();
        var invalidCommand = new RemoveEpisodePlatformCommand(Guid.Empty, Guid.Empty);

        // Act
        var result = validator.Validate(invalidCommand);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RemoveEpisodePlatformCommand.EpisodeId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RemoveEpisodePlatformCommand.PlatformId));
    }

    [Fact]
    public void RemoveEpisodePlatformValidator_WhenValid_ShouldPassValidation()
    {
        // Arrange
        var validator = new RemoveEpisodePlatformCommandValidator();
        var validCommand = new RemoveEpisodePlatformCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = validator.Validate(validCommand);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
