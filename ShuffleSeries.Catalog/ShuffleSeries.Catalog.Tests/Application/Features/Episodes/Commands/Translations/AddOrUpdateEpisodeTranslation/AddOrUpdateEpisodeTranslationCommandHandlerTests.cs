using Moq;
using ShuffleSeries.Catalog.Application.Features.Episodes.Commands.Translations.AddOrUpdateEpisodeTranslation;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Domain.Exceptions;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Application.Features.Episodes.Commands.Translations.AddOrUpdateEpisodeTranslation;

public class AddOrUpdateEpisodeTranslationCommandHandlerTests
{
    private readonly Mock<IEpisodeRepository> _episodeRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private static Episode CreateSampleEpisode()
    {
        var series = ShuffleSeries.Catalog.Domain.Entities.Series.Create(42009, "tt2085059", 2011, SeriesType.EpisodicAnthology, "https://example.com/poster.jpg");
        return series.AddEpisode(1, 1, isStandalone: true);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldAddTranslationAndSaveChanges()
    {
        var episode = CreateSampleEpisode();

        _episodeRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(episode.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(episode);

        var handler = new AddOrUpdateEpisodeTranslationCommandHandler(
            _episodeRepositoryMock.Object,
            _unitOfWorkMock.Object);

        var command = new AddOrUpdateEpisodeTranslationCommand(episode.Id, "tr", "Milli Marş", "Black Mirror 1. Bölüm");

        await handler.Handle(command, CancellationToken.None);

        episode.Translations.Should().HaveCount(1);
        episode.Translations.First().LanguageCode.Value.Should().Be("tr");
        episode.Translations.First().Title.Should().Be("Milli Marş");

        _episodeRepositoryMock.Verify(x => x.Update(episode), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEpisodeNotFound_ShouldThrowEpisodeNotFoundException()
    {
        var nonExistentId = Guid.NewGuid();
        _episodeRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Episode?)null);

        var handler = new AddOrUpdateEpisodeTranslationCommandHandler(
            _episodeRepositoryMock.Object,
            _unitOfWorkMock.Object);

        var command = new AddOrUpdateEpisodeTranslationCommand(nonExistentId, "tr", "Başlık");

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<EpisodeNotFoundException>();
    }
}
