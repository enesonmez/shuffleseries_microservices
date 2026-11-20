using Moq;
using ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Translations.RemoveEpisodeTranslation;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Episodes.Commands.Translations.RemoveEpisodeTranslation;

public class RemoveEpisodeTranslationCommandHandlerTests
{
    private readonly Mock<IEpisodeRepository> _episodeRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private static Episode CreateSampleEpisode()
    {
        var series = ShuffleSeries.Catalog.Domain.Entities.Series.Create(42009, "tt2085059", 2011, SeriesType.EpisodicAnthology, "https://example.com/poster.jpg");
        return series.AddEpisode(1, 1, isStandalone: true);
    }

    [Fact]
    public async Task Handle_WhenEpisodeExists_ShouldRemoveTranslationAndSaveChanges()
    {
        var episode = CreateSampleEpisode();
        episode.AddOrUpdateTranslation("en", "National Anthem", "Overview");

        _episodeRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(episode.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(episode);

        var handler = new RemoveEpisodeTranslationCommandHandler(
            _episodeRepositoryMock.Object,
            _unitOfWorkMock.Object);

        var command = new RemoveEpisodeTranslationCommand(episode.Id, "en");

        await handler.Handle(command, CancellationToken.None);

        episode.Translations.Should().BeEmpty();
        _episodeRepositoryMock.Verify(x => x.Update(episode), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEpisodeNotFound_ShouldThrowEpisodeNotFoundException()
    {
        var nonExistentId = Guid.NewGuid();
        _episodeRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Episode?)null);

        var handler = new RemoveEpisodeTranslationCommandHandler(
            _episodeRepositoryMock.Object,
            _unitOfWorkMock.Object);

        var command = new RemoveEpisodeTranslationCommand(nonExistentId, "en");

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<EpisodeNotFoundException>();
    }
}
