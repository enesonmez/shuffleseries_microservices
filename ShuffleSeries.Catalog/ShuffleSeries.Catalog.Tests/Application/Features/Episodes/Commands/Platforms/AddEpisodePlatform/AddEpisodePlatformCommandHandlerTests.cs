using Moq;
using ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Platforms.AddEpisodePlatform;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Episodes.Commands.Platforms.AddEpisodePlatform;

public class AddEpisodePlatformCommandHandlerTests
{
    private readonly Mock<IEpisodeRepository> _episodeRepositoryMock = new();
    private readonly Mock<IPlatformRepository> _platformRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private static Episode CreateSampleEpisode()
    {
        var series = ShuffleSeries.Catalog.Domain.Entities.Series.Create(42009, "tt2085059", 2011, SeriesType.EpisodicAnthology, "https://example.com/poster.jpg");
        return series.AddEpisode(1, 1, isStandalone: true);
    }

    private static Platform CreateSamplePlatform() =>
        Platform.Create("Netflix", "netflix", "https://netflix.com/logo.png", "https://netflix.com/off.png", "https://netflix.com");

    [Fact]
    public async Task Handle_WhenValid_ShouldAddPlatformAndSaveChanges()
    {
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

        await handler.Handle(command, CancellationToken.None);

        episode.EpisodePlatforms.Should().HaveCount(1);
        episode.EpisodePlatforms.First().PlatformId.Should().Be(platform.Id);
        episode.EpisodePlatforms.First().WebUrl.Should().Be("https://netflix.com/watch/123");
        episode.EpisodePlatforms.First().AppSchemeUrl.Should().Be("nflx://watch/123");

        _episodeRepositoryMock.Verify(x => x.Update(episode), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPlatformAlreadyExists_ShouldThrowDuplicateEpisodePlatformException()
    {
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

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateEpisodePlatformException>();
        _episodeRepositoryMock.Verify(x => x.Update(It.IsAny<Episode>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEpisodeNotFound_ShouldThrowEpisodeNotFoundException()
    {
        var nonExistentId = Guid.NewGuid();
        _episodeRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Episode?)null);

        var handler = new AddEpisodePlatformCommandHandler(
            _episodeRepositoryMock.Object,
            _platformRepositoryMock.Object,
            _unitOfWorkMock.Object);

        var command = new AddEpisodePlatformCommand(nonExistentId, Guid.NewGuid(), "https://netflix.com/watch/123");

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<EpisodeNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenPlatformNotFound_ShouldThrowPlatformNotFoundException()
    {
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

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<PlatformNotFoundException>();
    }
}
